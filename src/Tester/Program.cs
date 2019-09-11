using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Json.Net;
using KinesisSharp;
using LocalStack.Client;
using LocalStack.Client.Contracts;

namespace Tester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var awsAccessKeyId = "Key Id";
            var awsAccessKey = "Secret Key";
            var awsSessionToken = "Token";
            var regionName = "us-west-1";
            var localStackHost = "localhost";

            var streamName = "reader-stream";

            ISession session = SessionStandalone
                .Init()
                .WithSessionOptions(awsAccessKeyId, awsAccessKey, awsSessionToken, regionName)
                .WithConfig(localStackHost)
                .Create();

            //await PublishRecords(streamName, session.CreateClient<AmazonKinesisClient>());

            var worker = new Worker(streamName, session.CreateClient<AmazonKinesisClient>());

            await worker.RunAsync();
        }

        static async Task PublishRecords(String streamName, IAmazonKinesis client)
        {
            var numberOfRecords = 100;
            var partitions = new[] {1, 2, 3, 4};

            var records = Enumerable.Range(1, numberOfRecords).Select(i =>
                new Record {Partition = partitions[(i * 2343) % 4], OrderId = i, Extra = "Message: " + i});
                
                
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