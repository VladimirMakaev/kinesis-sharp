using System;
using System.Collections.Generic;

namespace KinesisSharp
{
    public class ShardRef
    {
        public ShardRef(string shardId, IEnumerable<string> parentShardIds)
        {
            ShardId = shardId;
            ParentShardIds = parentShardIds;
        }

        public IEnumerable<string> ParentShardIds { get; }

        public string ShardId { get; }
    }
}