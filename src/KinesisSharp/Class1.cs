using System.Collections.Generic;

namespace KinesisSharp
{
    public struct SequenceNumber
    {
        private readonly string sequenceNumber;

        public SequenceNumber(string sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }
    }


    public class ShardRef
    {
        private readonly string concurrencyToken;
        private readonly IEnumerable<string> parentShardIds;
        private readonly string shardId;

        public ShardRef(string shardId, string concurrencyToken, IEnumerable<string> parentShardIds)
        {
            this.shardId = shardId;
            this.concurrencyToken = concurrencyToken;
            this.parentShardIds = parentShardIds;
        }

        public string ConcurrencyToken => concurrencyToken;

        public IEnumerable<string> ParentShardIds => parentShardIds;

        public string ShardId => shardId;
    }
}