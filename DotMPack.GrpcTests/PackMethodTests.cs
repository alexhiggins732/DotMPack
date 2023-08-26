using System.Reflection;
using System.ServiceModel;
using Grpc.Net.Client;
using HigginsSoft.DotMPack.GrpcTests.CompiledServices.Dynamic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace HigginsSoft.DotMPack.GrpcTests
{
    namespace CompiledServices
    {
        [ServiceContract]
        public interface IConfigurationService
        {
            string? GetValue(string key);
            bool SetValue(string key, string value);
            bool SetValueWithObject(string key, string value);
            //List<string> Get(Func<string, bool> keySelector);
        }

        [ServiceContract]
        public interface IConfigurationServicePacked
        {
            Pack? GetValue(Pack request);
            Pack SetValue(Pack request);
            Pack SetValueWithObject(Pack request);
            //Pack Get(Pack keySelector);
        }

        //public interface IServiceProxy
        //{
        //    Pack? InvokePacked(MethodInfo target, params object[] args);
        //}


        //public interface IServiceProxy<T> : IServiceProxy
        //{
        //    T Proxy { get; set; }

        //}

        //public class ServiceProxy<T> : IServiceProxy<T>
        //{
        //    public T Proxy { get; set; }
        //    public Pack? InvokePacked(MethodInfo target, params object[] args)
        //    {
        //        var result = new Pack();
        //        return result;
        //    }
        //}


        namespace Server
        {
            public class ConfigurationService : IConfigurationService
            {
                static Dictionary<string, string> store = new();
                public string? GetValue(string key)
                {
                    store.TryGetValue(key, out var value);
                    return value;
                }

                public bool SetValue(string key, string value)
                {
                    store[key] = value;
                    return true;
                }
                public bool SetValueWithObject(string key, string value)
                {
                    store[key] = value;
                    return true;
                }
                public List<string> Get(Func<string, bool> keySelector)
                {
                    return store.Keys.Where(keySelector).Select(key => store[key]).ToList();
                }

            }

            public class FilterExpression
            {
                public AssemblyName AssemblyName { get; set; }
                public int TypeToken { get; set; }
                public int MethodToken { get; set;}
            }

            public class ConfigurationServicePacked : IConfigurationServicePacked
            {
                public ConfigurationServicePacked(IConfigurationService configService)
                {
                    ConfigService = configService;
                }

                public IConfigurationService ConfigService { get; }

                public Pack Get(Pack request)
                {
                    //DotMPackMap map = request;
                    //var filter = new FilterExpression();
                    //filter.AssemblyName = new AssemblyName(map[nameof(filter.AssemblyName)]);
                    //filter.TypeToken = map[nameof(filter.TypeToken)];
                    //filter.MethodToken = map[nameof(filter.MethodToken)];

                    //var assembly = Assembly.Load(filter.AssemblyName);

                    //var module = assembly.ManifestModule;
                    //var t= module.ResolveType(filter.TypeToken);
                    //var f = module.ResolveMethod(filter.MethodToken);
                    return string.Empty;
                }

                public Pack? GetValue(Pack request)
                {
                    DotMPack value = request;
                    string? key = value.ToOrDefault<string>();
                    return ConfigService.GetValue(request);
                }

                public Pack SetValue(Pack request)
                {
                    DotMPackMap map = request;
                    string key = map[nameof(key)].ToOrDefault<string>();
                    string value = map[nameof(value)].ToOrDefault<string>();
                    return ConfigService.SetValue(key, value);
                }
                public Pack SetValueWithObject(Pack request)
                {
                    var args = (DotMPack[])request;
                    return ConfigService.SetValueWithObject(args[0], args[1]);
                }
            }
        }

        namespace Client
        {
            public class ConfigurationService : IConfigurationService
            {
                public ConfigurationService(IConfigurationServicePacked client)
                {
                    Client = client;
                }

                public IConfigurationServicePacked Client { get; }

                public string? GetValue(string key)
                {
                    return Client.GetValue(key);
                }

                public bool SetValue(string key, string value)
                {
                    var map = new DotMPackMap();
                    map.Add(nameof(key), key);
                    map.Add(nameof(value), value);
                    return Client.SetValue(map);
                }
                public bool SetValueWithObject(string key, string value)
                {
                    return Client.SetValueWithObject(new object[] { key, value });
                }
            }

            public class ConfigurationServicePacked : IConfigurationServicePacked
            {
                private IConfigurationServicePacked ConfigService;

                public ConfigurationServicePacked()
                {
                    var channel = GrpcChannel.ForAddress(DotGrpcServices.DotGrpcEndpoint);
                    this.ConfigService = channel.CreateGrpcService<IConfigurationServicePacked>();
                }


                public Pack? GetValue(Pack request)
                {
                    return ConfigService.GetValue(request);
                }

                public Pack SetValue(Pack request)
                {
                    return ConfigService.SetValue(request);
                }

                public Pack SetValueWithObject(Pack request)
                {
                    return ConfigService.SetValueWithObject(request);
                }
            }




        }



        namespace Dynamic
        {
            public class DotRpcChannel : IDisposable
            {
                private readonly string endpoint;
                private readonly GrpcChannel channel;


                public DotRpcChannel(string endpoint)
                {
                    this.endpoint = endpoint;
                    this.channel = GrpcChannel.ForAddress(endpoint);
                }

                public static DotRpcChannel ForAddress(string endpoint)
                    => new DotRpcChannel(endpoint);

                public void Dispose()
                {
                    channel.Dispose();
                }

                public T CreateRpcService<T>()
                {
                    return ServiceClientProxyFactory.CreateRpcService<T>();
                }
            }

            public class PackServiceInvoker
            {
                public IServiceProvider serviceProvider;
                public PackServiceInvoker(IServiceProvider serviceProvider)
                {
                    this.serviceProvider = serviceProvider;
                }

                public Pack Invoke<TService>(Func<TService, Pack, Pack> invoker, Pack request)
                    where TService : class
                {
                    var clientService = serviceProvider.GetRequiredService<TService>();
                    var result = invoker(clientService, request);
                    return result;
                }
            }

            public interface IProxyClient<T>
            {
                T Client { get; set; }
            }



            public interface IConfigurationServiceProxy : IConfigurationService { }

            public class ConfigurationServiceClient : IConfigurationService
            {
                IConfigurationServiceProxy serviceProxy;
                public ConfigurationServiceClient(IConfigurationServiceProxy serviceProxy)
                {
                    this.serviceProxy = serviceProxy;
                }

                public string? GetValue(string key)
                     => serviceProxy.GetValue(key);

                public bool SetValue(string key, string value)
                    => serviceProxy.SetValue(key, value);

                public bool SetValueWithObject(string key, string value)
                    => serviceProxy.SetValueWithObject(key, value);
            }

            //Dynamically Generated
            public class ConfigurationServiceProxy : IConfigurationServiceProxy
            {
                public PackServiceInvoker packService;
                public ConfigurationServiceProxy(PackServiceInvoker packService)
                {
                    this.packService = packService;
                }
                public string? GetValue2(string key)
                {
                    Func<IConfigurationPackedProxyService, Pack, Pack> fun = (a, b) => a.GetValue(b);
                    return packService.Invoke(fun, key);

                }
                public string? GetValue(string key)
                    => packService.Invoke<IConfigurationPackedProxyService>((client, request) => client.GetValue(request),
                        (DotMPackArray)new object[] { key });

                public bool SetValue(string key, string value)
                {
                    return packService.Invoke(
                        (IConfigurationPackedProxyService client, Pack request) => client.SetValue(request),
                        (DotMPackArray)new object[] { key, value });
                }

                public bool SetValue2(string key, string value)
                {
                    Func<IConfigurationPackedProxyService, Pack, Pack> fun = (a, b) => a.GetValue(b);
                    return packService.Invoke(fun, new object[] { key, value });

                }

                public bool SetValueWithObject(string key, string value)
                {
                    Func<IConfigurationPackedProxyService, Pack, Pack> fun = (a, b) => a.SetValueWithObject(b);
                    return packService.Invoke(fun, new object[] { key, value });

                }
            }


            // Dynamically Generated
            public interface IConfigurationPackedProxyService: IConfigurationServicePacked
            {
                //public Pack GetValue(Pack request);
                //public Pack SetValue(Pack request);
            }

            // Dynamically Generated
            public class ConfigurationPackedProxyServiceClient : IProxyClient<IConfigurationPackedProxyService>
                , IConfigurationPackedProxyService
            {

                public ConfigurationPackedProxyServiceClient()
                {
                    var channel = GrpcChannel.ForAddress(DotGrpcServices.DotGrpcEndpoint);
                    Client = channel.CreateGrpcService<IConfigurationPackedProxyService>();
                }

                public IConfigurationPackedProxyService Client { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

                public Pack GetValue(Pack request)
                {
                    return Client.GetValue(request);
                }

                public Pack SetValue(Pack request)
                {
                    return Client.SetValue(request);
                }

                public Pack SetValueWithObject(Pack request)
                {
                    return Client.SetValueWithObject(request);
                }
            }

            //Dynamically Generated
            public class ConfigurationPackedProxyService : IConfigurationPackedProxyService
            {
                readonly IConfigurationPackedProxyService configService;

                public ConfigurationPackedProxyService(IProxyClient<IConfigurationPackedProxyService> configService)
                {
                    this.configService = configService.Client;
                }

                public Pack? GetValue(Pack request)
                {
                    return configService.GetValue(request);
                }

                public Pack SetValue(Pack request)
                {
                    return configService.SetValue(request);
                }

                public Pack SetValueWithObject(Pack request)
                {
                    return configService.SetValueWithObject(request);
                }
            }

            public class ServiceClientProxyFactory
            {


                public static T CreateRpcService<T>()
                {
                    if (
                        (typeof(T) == typeof(IConfigurationService)) // T is IConfigurationService
                        || typeof(T).GetInterface(nameof(IConfigurationService)) != null // T implements IConfigurationService
                        || typeof(IConfigurationService).IsAssignableFrom(typeof(T)) // T implements or is derived from IConfigurationService
                        )
                    {
                        return (T)DotGrpcServices.ClientServiceProvider.GetRequiredService<IConfigurationService>();
                        //return (T)(IConfigurationService)new ConfigurationServiceProxy(new PackServiceInvoker(DotGrpcServices.ClientServiceProvider));
                    }
                    throw new NotImplementedException();
                }
            }


        }

        public static class DotRpcExtenstions
        {
            public static IServiceCollection ConfigureDotGrpcClient(this IServiceCollection services, Action<IServiceCollection>? configure = null)
            {
                configure?.Invoke(services);
                services.AddSingleton<IConfigurationService, Client.ConfigurationService>();
                services.AddSingleton<IConfigurationServicePacked, Client.ConfigurationServicePacked>();
                DotGrpcServices.ClientServices = services;
                return services;
            }

            public static IServiceCollection ConfigureDotGrpcServer(this IServiceCollection services, Action<IServiceCollection>? configure = null)
            {
                configure?.Invoke(services);
                services.AddSingleton<IConfigurationService, Server.ConfigurationService>();
                services.AddSingleton<IConfigurationServicePacked, Server.ConfigurationServicePacked>();
                DotGrpcServices.ClientServices = services;
                return services;
            }


            public static WebApplication ConfigureDotGrpc(this WebApplication app, Action<WebApplication>? configure = null)
            {
                configure?.Invoke(app);
                DotGrpcServices.App = app;
                app.MapGrpcService<Server.ConfigurationServicePacked>();
                return app;
            }
        }


        public class DotGrpcServices
        {
            internal static IServiceCollection Services { get; set; }
            private static IServiceProvider serviceProvider;
            internal static IServiceProvider ServiceProvider => serviceProvider ?? (serviceProvider = Services.BuildServiceProvider());

            public static WebApplication App { get; internal set; }
            public static string DotGrpcEndpoint = "https://localhost:7187";

            public static ServiceProvider ClientServiceProvider { get; internal set; }
            public static IServiceCollection ClientServices { get; internal set; }
        }




        public class ServerFixture : IDisposable
        {
            public static bool Started;

            public static string GrpcTestUrl = DotGrpcServices.DotGrpcEndpoint;
            WebApplication Server;
            public ServerFixture()
            {
                if (Started && Server is not null)
                {
                    return;
                }

                Started = true;
                var builder = WebApplication.CreateBuilder(new string[] { });
                builder.WebHost.UseUrls(GrpcTestUrl);

                // Additional configuration is required to successfully run gRPC on macOS.
                // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

                // Add services to the container.
                builder.Services.ConfigureDotGrpcServer();
                builder.Services.AddCodeFirstGrpc();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {

                });
                var app = builder.Build();
                if (app is null)
                    throw new Exception("Failed to build Web Application");
                // Configure the HTTP request pipeline.
                // app.MapPost("api/GetValue", (IConfigurationService svc, string ) => svc.GetValue(request));
                //app.MapPost("api/SetValue", (IPackedConfigurationService svc, Pack request) => svc.SetValue(request));
                app.UseSwagger();
                app.UseSwaggerUI(x => { });

                app.ConfigureDotGrpc();
                Server = app;
                app.RunAsync();
            }

            public void Dispose()
            {
                Server.StopAsync().Wait();
            }
        }

        [TestClass]
        public class DotRpcPackedClientTests : IDisposable
        {
            private ServiceProvider serviceProvider;
            private ServerFixture serverFixture;

            public DotRpcPackedClientTests()
            {
                var services = new ServiceCollection();

                services.ConfigureDotGrpcClient();



                var provider = services.BuildServiceProvider();
                this.serviceProvider = provider;
                DotGrpcServices.ClientServiceProvider = serviceProvider;
                this.serverFixture = new ServerFixture();
            }

            public void Dispose()
            {
                ((IDisposable)serverFixture).Dispose();
            }

            [TestMethod]
            public void TestDotMPackService()
            {


                using var channel = DotRpcChannel.ForAddress(ServerFixture.GrpcTestUrl);
                var client = channel.CreateRpcService<IConfigurationService>();

                string testKey = nameof(testKey);
                string testValue = nameof(testValue);

                var result = client.GetValue(testKey);
                Assert.IsNull(result);

                client.SetValue(testKey, testValue);

                var value = client.GetValue(testKey);

                Console.WriteLine($"Expected: {testValue} - Value: {value}");
                Assert.AreEqual(testValue, value);

                string testUpdateValue = nameof(testUpdateValue);

                client.SetValue(testKey, testUpdateValue);

                value = client.GetValue(testKey);
                Console.WriteLine($"Expected: {testUpdateValue} - Value: {value}");
                Assert.AreEqual(testUpdateValue, value);

                string testUpdateValueWithObject = nameof(testUpdateValueWithObject);

                client.SetValueWithObject(testKey, testUpdateValueWithObject);

                value = client.GetValue(testKey);
                Console.WriteLine($"Expected: {testUpdateValueWithObject} - Value: {value}");
                Assert.AreEqual(testUpdateValueWithObject, value);
            }
        }
    }



}

