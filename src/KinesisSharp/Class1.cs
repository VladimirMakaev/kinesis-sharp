using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;

namespace KinesisSharp
{
    public class SequenceNumber
    {
        private readonly string sequenceNumber;

        public SequenceNumber(String sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }
    }


    public class ShardRef
    {
        private readonly string shardId;
        private readonly string concurrencyToken;
        private readonly IEnumerable<string> parentShardIds;

        public ShardRef(String shardId, String concurrencyToken, IEnumerable<String> parentShardIds)
        {
            this.shardId = shardId;
            this.concurrencyToken = concurrencyToken;
            this.parentShardIds = parentShardIds;
        }
    }

    public class Worker
    {
        private readonly string streamName;
        private readonly IAmazonKinesis amazonKinesisClient;

        public Worker(string streamName, IAmazonKinesis amazonKinesisClient)
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

                if (records.Records.Count == 0)
                {
                    break;
                }
            }
        }
    }
}
