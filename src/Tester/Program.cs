using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Json.Net;
using KinesisSharp;
using KinesisSharp.Configuration;
using KinesisSharp.Leases;
using KinesisSharp.Leases.Discovery;
using KinesisSharp.Leases.Lock;
using KinesisSharp.Leases.Registry;
using KinesisSharp.Processor;
using KinesisSharp.Records;
using KinesisSharp.Shards;
using LocalStack.Client;
using LocalStack.Client.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tests.Mocks;

namespace Tester
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //await PublishRecords("reader-stream", CreateLocalStackSession().CreateClient<AmazonKinesisClient>());
            var host = new HostBuilder()
                .ConfigureAppConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Logging:LogLevel:Default", "Debug"},
                    {"Kinesis:NumberOfWorkers", "2"},
                    {"Kinesis:StreamArn", "reader-stream"},
                    {"Kinesis:ApplicationName", "tester-application"}
                }))
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync().ConfigureAwait(false);

            //var worker = new WorkerService(streamName, session.CreateClient<AmazonKinesisClient>());

            //await worker.RunAsync();
        }

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            var session = CreateLocalStackSession();

            services.AddOptions();

            services.AddSingleton<IDiscoverShards>(p =>
            {
                var shards = new MockShardCollection();
                shards.Create("shard-1", 0, 10);
                shards.Create("shard-2", 10, 20);
                shards.Create("shard-3", 30, 40);
                shards.Create("shard-4", 50, 60);
                shards.Create("shard-5", 60, 70);
                shards.Create("shard-6", 70, 80);
                shards.Merge("shard-7", "shard-3", "shard-4", 100);
                shards.Merge("shard-8", "shard-7", "shard-5", 200);
                shards.Merge("shard-9", "shard-1", "shard-2", 300);
                shards.RemoveAt(0); //Make shard broken
                return new InMemoryDiscoverShards(shards);
            });

            services.Configure<ApplicationConfiguration>(host.Configuration.GetSection("Kinesis"));
            services.Configure<StreamConfiguration>(host.Configuration.GetSection("Kinesis"));

            services.AddLogging(b => b.AddConfiguration(host.Configuration.GetSection("Logging")).AddConsole());
            services.AddSingleton<IAmazonKinesis>(session.CreateClient<AmazonKinesisClient>());

            //services.AddHostedService<WorkerScheduler>();
//            services.AddHostedService<WorkerService>();
            //          services.AddHostedService<WorkerService>();
            //        services.AddHostedService<WorkerService>();
            services.AddHostedService<WorkerService2>();
            services.AddHostedService<LeaseDiscoveryWorker>();

            services.AddSingleton<ILeaseDiscoveryService, LeaseDiscoveryService>();
            services.AddSingleton<IKinesisShardReaderFactory, KinesisShardReaderFactory>();
            services.AddSingleton<IDistributedLockService, InMemoryLockService>();
            services.AddSingleton<ILeaseClaimingService, LeaseClaimingService>();
            services.AddSingleton<InMemoryLeaseRegistry>();

            services.AddSingleton<ILeaseRegistryQuery>(p => p.GetRequiredService<InMemoryLeaseRegistry>());
            services.AddSingleton<ILeaseRegistryCommand>(p => p.GetRequiredService<InMemoryLeaseRegistry>());

            services.AddSingleton<IRecordsProcessor, SampleProcessor>();
            //services.AddSingleton<IDiscoverShards, DiscoverShards>();
        }

        private static ISession CreateLocalStackSession()
        {
            var awsAccessKeyId = "Key LockId";
            var awsAccessKey = "Secret Key";
            var awsSessionToken = "Token";
            var regionName = "us-west-1";
            var localStackHost = "localhost";

            var session = SessionStandalone
                .Init()
                .WithSessionOptions(awsAccessKeyId, awsAccessKey, awsSessionToken, regionName)
                .WithConfig(localStackHost)
                .Create();
            return session;
        }

        private static async Task PublishRecords(string streamName, IAmazonKinesis client)
        {
            var numberOfRecords = 100;
            var partitions = new[] {1, 2, 3, 4};

            var records = Enumerable.Range(1, numberOfRecords).Select(i =>
                new Record {Partition = partitions[i * 2343 % 4], OrderId = i, Extra = "Message: " + i});


            var result = await client.PutRecordsAsync(new PutRecordsRequest
            {
                Records = records.Select(r => new PutRecordsRequestEntry
                {
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonNet.Serialize(r))),
                    PartitionKey = r.Partition.ToString()
                }).ToList(),
                StreamName = streamName
            }).ConfigureAwait(false);

            Console.WriteLine(JsonNet.Serialize(result));
        }
    }

    public class Record
    {
        public int Partition { get; set; }
        public int OrderId { get; set; }
        public string Extra { get; set; }
    }
}