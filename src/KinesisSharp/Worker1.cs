using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using KinesisSharp.Shards;

namespace KinesisSharp
{
    public class Coordinator
    {
        private readonly IShardEnumerator shardEnumerator;

        public Coordinator(IShardEnumerator shardEnumerator)
        {
            this.shardEnumerator = shardEnumerator;
        }


        public async Task RunAsync(CancellationToken token)
        {
            var shards = await this.shardEnumerator.GetShards(token);

            



        }
    }


    public class Worker1
    {
        private readonly IAmazonKinesis amazonKinesisClient;
        private readonly string streamName;

        public Worker1(string streamName, IAmazonKinesis amazonKinesisClient)
        {
            this.streamName = streamName;
            this.amazonKinesisClient = amazonKinesisClient;
        }

        public async Task RunAsync()
        {
            var shards = await amazonKinesisClient.ListShardsAsync(new ListShardsRequest
            {
                StreamName = streamName
            });

            var shard1 = shards.Shards[3];

            var iteratorResponse = await amazonKinesisClient.GetShardIteratorAsync(new GetShardIteratorRequest
            {
                ShardId = shard1.ShardId,
                ShardIteratorType = ShardIteratorType.AT_SEQUENCE_NUMBER,
                StartingSequenceNumber = shard1.SequenceNumberRange.StartingSequenceNumber,
                StreamName = streamName
            });


            var iterator = iteratorResponse.ShardIterator;

            while (true)
            {
                var records = await amazonKinesisClient.GetRecordsAsync(new GetRecordsRequest
                {
                    Limit = 10,
                    ShardIterator = iterator
                });

                iterator = records.NextShardIterator;

                Console.WriteLine("REtrieved: " + records.Records.Count);

                if (records.Records.Count == 0) break;
            }
        }
    }
}