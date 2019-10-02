using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Json.Net;
using KinesisSharp;
using KinesisSharp.Configuration;
using KinesisSharp.Leases;
using KinesisSharp.Leases.Discovery;
using KinesisSharp.Leases.Lock;
using KinesisSharp.Leases.Lock.Redis;
using KinesisSharp.Leases.Registry;
using KinesisSharp.Leases.Registry.Redis;
using KinesisSharp.Processor;
using KinesisSharp.Records;
using KinesisSharp.Shards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Tester.Processor;
using Tests.Mocks;

namespace Tester
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(b => b.AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Logging:LogLevel:Default", "Debug"},
                    {"Kinesis:NumberOfWorkers", "5"},
                    {"Kinesis:RecordsBatchLimit", "1000"},
                    {"Kinesis:StreamArn", "shard-test-1"},
                    {"Kinesis:ApplicationName", "shard-test-application"}
                }).AddEnvironmentVariables())
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync().ConfigureAwait(false);
        }

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.AddOptions();

            services.Configure<ApplicationConfiguration>(host.Configuration.GetSection("Kinesis"));
            services.Configure<StreamConfiguration>(host.Configuration.GetSection("Kinesis"));

            services.AddLogging(b =>
            {
                b.AddConfiguration(host.Configuration.GetSection("Logging"));
                b.AddConsole();
            });
            services.AddHostedService<WorkerService>();
            services.AddHostedService<LeaseDiscoveryWorker>();

            services.AddSingleton<ILeaseDiscoveryService, LeaseDiscoveryService>();
            services.AddSingleton<ILeaseClaimingService, LeaseClaimingService>();

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
                return new InMemoryDiscoverShards(shards);
            });
            services.AddSingleton<IKinesisShardReaderFactory>(new StubKinesisShardReaderFactory(1000, 100));
            services.AddSingleton<InMemoryLeaseRegistry>();
            services.AddSingleton<ILeaseRegistryQuery, RedisLeaseRegistryQuery>();
            services.AddSingleton<ILeaseRegistryCommand, RedisRegistryLeaseCommand>();
            //services.AddSingleton<IDistributedLockService, InMemoryLockService>();
            services.AddSingleton<IDistributedLockService, RedisDistributedLock>();

            //services.AddLocalStack();
            //services.AddSingleton<IRecordsProcessor, SampleProcessor>();
            services.AddSingleton<IRecordsProcessor, ProcessorWithInvariantCheck>();

            services.AddSingleton<IConnectionMultiplexer>(x =>
                ConnectionMultiplexer.Connect(host.Configuration.GetValue<string>("Redis:ConnectionString")));


            if (host.Configuration.GetValue<bool>("RealMode"))
            {
                services.AddSingleton<IAmazonKinesis>(p =>
                    new AmazonKinesisClient(RegionEndpoint.EUWest1));
                services.AddSingleton<IDiscoverShards, DiscoverShards>();
                services.AddSingleton<IKinesisShardReaderFactory, KinesisShardReaderFactory>();
            }
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
