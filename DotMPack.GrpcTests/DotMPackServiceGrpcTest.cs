using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace HigginsSoft.DotMPack.GrpcTests
{
    [ServiceContract]
    public interface IPackedConfigurationService
    {
        Pack GetValue(Pack request);
        Pack SetValue(Pack request);

    }
    public class PackedConfigurationService : IPackedConfigurationService
    {
        static DotMPackMap store = new();
        public Pack GetValue(Pack packed)
        {
            DotMPack request = packed;
            var key = request.To<string>();
            if (store.ContainsKey(key))
                return store[key];
            return DotMPack.Null();

        }

        public Pack GetValue2(Pack request)
        {
            var payload = request.Payload;
            var pack = DotMPack.ParseFromBytes(payload.ToArray());
            var key = pack.To<string>();
            if (store.ContainsKey(key))
                return store[key];
            return DotMPack.Null();

        }

        public Pack SetValue(Pack packedRequest)
        {
            DotMPackMap map = packedRequest;
            string key = map[nameof(key)].To<string>();
            DotMPack value = map[nameof(value)];
            store[key] = value;
            return value;
        }
    }

    public class ServerFixture : IDisposable
    {
        public static bool Started;

        public static string GrpcTestUrl = "https://localhost:7186";
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
            builder.Services.AddSingleton<IPackedConfigurationService, PackedConfigurationService>();
            // Add services to the container.
            builder.Services.AddCodeFirstGrpc();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {

            });
            var app = builder.Build();
            if (app is null)
                throw new Exception("Failed to build Web Application");
            // Configure the HTTP request pipeline.
            app.MapPost("api/GetValue", (IPackedConfigurationService svc, DotMPack key) => svc.GetValue(key));
            app.MapPost("api/SetValue", (IPackedConfigurationService svc, DotMPackMap keyValue) => svc.SetValue(keyValue));
            app.UseSwagger();
            app.UseSwaggerUI(x => { });
            app.MapGrpcService<PackedConfigurationService>();
            Server = app;
            app.RunAsync();
        }

        public void Dispose()
        {
            Server.StopAsync().Wait();
        }
    }


    

    [TestClass]
    public class DotMPackServiceGrpcTest : IDisposable
    {
        private ServerFixture server;

        public DotMPackServiceGrpcTest()
        {
            this.server = new ServerFixture();
        }

        public void Dispose()
        {
            ((IDisposable)server).Dispose();
        }

        [TestMethod]
        public void TestDotMPackService()
        {


            using var channel = GrpcChannel.ForAddress(ServerFixture.GrpcTestUrl);
            var client = channel.CreateGrpcService<IPackedConfigurationService>();

            string testKey = nameof(testKey);
            string testValue = nameof(testValue);
            var setRequest = new DotMPackMap
            {
                { "key", testKey },
                { "value", testValue }
            };

            var result = client.GetValue(testKey);
            Assert.IsNull(((DotMPack)result).Value);
            var payload = setRequest.Payload;
            client.SetValue(setRequest);

            var getValueResult = client.GetValue(testKey);
            string value = getValueResult;
            Console.WriteLine($"Expected: {testValue} - Value: {value}");
            Assert.AreEqual(testValue, value);

            string testUpdateValue = nameof(testUpdateValue);
            var updateRequest = new DotMPackMap
            {
                { "key", testKey },
                { "value", testUpdateValue }
            };

            client.SetValue(updateRequest);

            value = client.GetValue(testKey);
            Console.WriteLine($"Expected: {testUpdateValue} - Value: {value}");
            Assert.AreEqual(testUpdateValue, value);
        }
    }
}
