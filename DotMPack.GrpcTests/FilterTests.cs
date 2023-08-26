using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System;
using Grpc.Net.Client.Configuration;

namespace HigginsSoft.DotMPack.GrpcTests
{

    public interface IMemoryCache
    {
        public void Add(string key, string value);
        public string? Get(string key);
        public List<string> List(Func<string, bool> predicate);
        public List<string> List2(FilterExpression expression);
    }


    [TestClass]
    public class FilterTests
    {
        private ServiceProvider serviceProvider;

        public FilterTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton(this);
            services.AddSingleton<IMemoryCache, InMemoryCache>();
            this.serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public void TestFilterFunc()
        {
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();

            string[] values = new[] { "test", "test1", "test2", "service1:key1", "service1:key2", "service2:key1", "service2:key2" };
            values.ToList().ForEach(value => cache.Add(value, value));

            var filtered = cache.List(Filter1);
            Assert.IsTrue(filtered.Any());
            Assert.IsTrue(filtered.Count == 2);
        }

        [TestMethod]
        public void TestFilterDelegate()
        {
            var cache = serviceProvider.GetRequiredService<IMemoryCache>();

            string[] values = new[] { "test", "test1", "test2", "service1:key1", "service1:key2", "service2:key1", "service2:key2" };
            values.ToList().ForEach(value => cache.Add(value, value));


            var filterExpression = new FilterExpression(Filter1);


            var filtered = cache.List2(filterExpression);
            Assert.IsTrue(filtered.Any());
            Assert.IsTrue(filtered.Count == 2);
        }

        private bool Filter1(string key)
        {
            return key.StartsWith("Service2", StringComparison.InvariantCultureIgnoreCase);
        }
    }



    public class FilterExpression
    {
    

        public AssemblyName AssemblyName { get; private set; }
        public int TypeToken { get; private set; }
        public int MethodToken { get; private set; }

        public Assembly Assembly => Assembly.Load(AssemblyName);
        public Type Type => Assembly.ManifestModule.ResolveType(TypeToken);
        public MethodInfo Method => (MethodInfo)Assembly.ManifestModule.ResolveMethod(MethodToken);


        public FilterExpression() { }

        public FilterExpression(Delegate @delegate) : this(@delegate.Method) { }

        public FilterExpression(MethodInfo method)
        {
            AssemblyName = method.DeclaringType.Assembly.GetName();
            TypeToken = method.DeclaringType.MetadataToken;
            MethodToken = method.MetadataToken;
        }


    }
 


    public class InMemoryCache : IMemoryCache
    {
        public static Dictionary<string, string> store = new();
        private readonly IServiceProvider serviceProvider;

        public InMemoryCache(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }


        public List<string> Query(IQueryable query)
        {
            var results = store.Where(x => MatchesQuery(query, x)).Select(x=> x.Value).ToList();
            return results;
        }

        public List<string> Query(FilterExpression filter)
        {

            var assemblyName = filter.AssemblyName;
            var assembly = Assembly.Load(assemblyName);
            var module = assembly.ManifestModule;
            var type = module.ResolveType(filter.TypeToken);
            var method = module.ResolveMethod(filter.MethodToken);

            IQueryable query = store.AsQueryable();
            //Expression exp = method.Invoke;

            //query = query.Provider.CreateQuery(method);


            var results = store.Where(x => MatchesQuery(query, x)).Select(x => x.Value).ToList();
            return results;
        }

      

        private bool MatchesQuery(IQueryable query, KeyValuePair<string, string> item)
        {
            // Assuming your query parameter is an Expression<Func<KeyValuePair<string, string>, bool>>
            var compiledQuery = (Expression<Func<KeyValuePair<string, string>, bool>>)query.Expression;
            var compiledFunc = compiledQuery.Compile();

            return compiledFunc(item);
        }




        public void Add(string key, string value)
        {
            store.Add(key, value);
        }

        public string? Get(string key)
        {
            store.TryGetValue(key, out var value);
            return value;
        }

        public List<string> List(Func<string, bool> predicate)
        {
            var kvp = store.ToList();
            var result = kvp.Where(x => predicate(x.Key)).Select(x => x.Value).ToList();
            return result;
        }

        public List<string> List2(FilterExpression filter)
        {
            var assemblyName = filter.AssemblyName;
            var assembly = Assembly.Load(assemblyName);
            var module = assembly.ManifestModule;
            var type = module.ResolveType(filter.TypeToken);
            var method = module.ResolveMethod(filter.MethodToken);

            Func<string, bool> predicate = null!;
            if (method.IsStatic)
            {
                predicate = x => (bool)method.Invoke(null, new object[] { x });
            }
            else
            {
                var instance = serviceProvider.GetRequiredService(type);
                predicate = x => (bool)method.Invoke(instance, new object[] { x });
            }

            var kvp = store.ToList();
            var result = kvp.Where(x => predicate(x.Key)).Select(x => x.Value).ToList();
            return result;
        }

    }


}

