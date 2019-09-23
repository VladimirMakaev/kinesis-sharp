using System;

namespace KinesisSharp.Leases
{
    public class Lease
    {
        public string ShardId { get; set; }

        public string Owner { get; set; }

        public ShardPosition Checkpoint { get; set; }

        public DateTime LastUpdate { get; set; }

        public string RowVersion { get; set; }

        public DateTime? LockExpiresOn { get; set; }
    }
}