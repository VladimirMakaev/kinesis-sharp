using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Shards
{
    public class ShardMap : ReadOnlyDictionary<string, Shard>
    {
        public ShardMap(IEnumerable<Shard> shards) : base(shards.ToDictionary(s => s.ShardId, s => s))
        {
        }

        public bool IsBroken(Shard shard)
        {
            if (!string.IsNullOrEmpty(shard.AdjacentParentShardId) &&
                !Dictionary.ContainsKey(shard.AdjacentParentShardId))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(shard.ParentShardId) &&
                !Dictionary.ContainsKey(shard.ParentShardId))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<Shard> GetBrokenShards()
        {
            foreach (var shard in Dictionary.Values)
            {
                if (!string.IsNullOrEmpty(shard.AdjacentParentShardId) &&
                    !Dictionary.ContainsKey(shard.AdjacentParentShardId))
                {
                    yield return shard;
                }

                if (!string.IsNullOrEmpty(shard.ParentShardId) &&
                    !Dictionary.ContainsKey(shard.ParentShardId))
                {
                    yield return shard;
                }
            }
        }

        public IEnumerable<Shard> SelectAllAncestors(Shard shard)
        {
            var result = new HashSet<string>();

            foreach (var s in SelectAllAncestorsRecursive(shard))
            {
                if (!result.Contains(s.ShardId))
                {
                    result.Add(s.ShardId);
                    yield return s;
                }
            }
        }

        private IEnumerable<Shard> SelectAllAncestorsRecursive(Shard shard)
        {
            if (shard.ParentShardId != null && ContainsKey(shard.ParentShardId))
            {
                yield return this[shard.ParentShardId];

                foreach (var parent in SelectAllAncestorsRecursive(this[shard.ParentShardId]))
                {
                    yield return parent;
                }
            }

            if (shard.AdjacentParentShardId != null && ContainsKey(shard.AdjacentParentShardId))
            {
                yield return this[shard.AdjacentParentShardId];

                foreach (var parent in SelectAllAncestorsRecursive(this[shard.AdjacentParentShardId]))
                {
                    yield return parent;
                }
            }
        }
    }
}