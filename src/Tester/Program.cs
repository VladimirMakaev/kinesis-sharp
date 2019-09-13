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
using LocalStack.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                    {"Scheduler:NumberOfWorkers", "4"}
                }))
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();

            //await PublishRecords(streamName, session.CreateClient<AmazonKinesisClient>());

            //var worker = new Worker1(streamName, session.CreateClient<AmazonKinesisClient>());

            //await worker.RunAsync();
        }

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            var awsAccessKeyId = "Key Id";
            var awsAccessKey = "Secret Key";
            var awsSessionToken = "Token";
            var regionName = "us-west-1";
            var localStackHost = "localhost";

            var streamName = "reader-stream";

            var session = SessionStandalone
                .Init()
                .WithSessionOptions(awsAccessKeyId, awsAccessKey, awsSessionToken, regionName)
                .WithConfig(localStackHost)
                .Create();

            services.AddOptions();

            services.Configure<WorkerSchedulerConfiguration>(host.Configuration.GetSection("Scheduler"));

            services.AddLogging(b => b.AddConfiguration(host.Configuration.GetSection("Logging")).AddConsole());

            services.AddSingleton<IAmazonKinesis>(session.CreateClient<AmazonKinesisClient>());

            services.AddSingleton<IHostedService, WorkerScheduler>();
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
            });

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