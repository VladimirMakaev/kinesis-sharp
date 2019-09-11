using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly Amazon.Kinesis.IAmazonKinesis amazonKinesisClient;

        public Worker(Amazon.Kinesis.IAmazonKinesis amazonKinesisClient)
        {
            this.amazonKinesisClient = amazonKinesisClient;
        }

        public Task RunAsync()
        {
            while (true)
            {
                this.amazonKinesisClient.GetShardIteratorAsync()
            }
        }
    }
}
