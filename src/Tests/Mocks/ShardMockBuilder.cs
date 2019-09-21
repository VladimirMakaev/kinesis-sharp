using Amazon.Kinesis.Model;

namespace Tests.Mocks
{
    public static class Shards
    {
        public static Shard Create(string id, long fromHash, long? toHash)
        {
            return new Shard
            {
                ShardId = id,
                AdjacentParentShardId = null,
                ParentShardId = null,
                HashKeyRange = new HashKeyRange
                {
                    StartingHashKey = fromHash.ToString(),
                    EndingHashKey = toHash?.ToString()
                },
                SequenceNumberRange = new SequenceNumberRange
                {
                    StartingSequenceNumber = "0"
                }
            };
        }

        public static Shard Merge(string id, Shard shard1, Shard shard2, long atPosition)
        {
            shard1.SequenceNumberRange.EndingSequenceNumber = atPosition.ToString();
            shard2.SequenceNumberRange.EndingSequenceNumber = atPosition.ToString();

            return new Shard
            {
                ShardId = id,
                ParentShardId = shard1.ShardId,
                AdjacentParentShardId = shard2.ShardId,
                HashKeyRange = new HashKeyRange
                {
                    StartingHashKey = shard1.HashKeyRange?.StartingHashKey,
                    EndingHashKey = shard2.HashKeyRange?.EndingHashKey
                }
            };
        }

        public static (Shard First, Shard Second) Split(Shard shard, string position, string id1, string id2,
            long atHash)
        {
            return (
                new Shard
                {
                    ShardId = id1,
                    ParentShardId = shard.ShardId,
                    AdjacentParentShardId = null,
                    HashKeyRange = new HashKeyRange
                    {
                        StartingHashKey = shard.HashKeyRange.StartingHashKey,
                        EndingHashKey = atHash.ToString()
                    },
                    SequenceNumberRange = new SequenceNumberRange
                    {
                        StartingSequenceNumber = position
                    }
                },
                new Shard
                {
                    ShardId = id2,
                    ParentShardId = shard.ShardId,
                    AdjacentParentShardId = null,
                    HashKeyRange = new HashKeyRange
                    {
                        StartingHashKey = atHash.ToString(),
                        EndingHashKey = shard.HashKeyRange.EndingHashKey
                    },
                    SequenceNumberRange = new SequenceNumberRange
                    {
                        StartingSequenceNumber = position
                    }
                }
            );
        }
    }
}