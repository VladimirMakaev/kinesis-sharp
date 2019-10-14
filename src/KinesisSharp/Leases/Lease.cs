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


        public static Lease New(Lease lease)
        {
            return new Lease
            {
                ShardId = lease.ShardId,
                Checkpoint = lease.Checkpoint,
                Owner = lease.Owner,
                LastUpdate = lease.LastUpdate,
                LockExpiresOn = lease.LockExpiresOn,
                RowVersion = lease.RowVersion
            };
        }
    }

    public class LeaseBuilder
    {
        private readonly Lease innerLease = new Lease();


        public LeaseBuilder()
        {
        }

        public LeaseBuilder(Lease lease)
        {
            innerLease.ShardId = lease.ShardId;
            innerLease.Checkpoint = lease.Checkpoint;
            innerLease.LastUpdate = lease.LastUpdate;
            innerLease.LockExpiresOn = lease.LockExpiresOn;
            innerLease.Owner = lease.Owner;
            innerLease.RowVersion = lease.RowVersion;
        }


        public LeaseBuilder WithOwner(string owner)
        {
            innerLease.Owner = owner;
            return this;
        }

        public Lease Build()
        {
            return innerLease;
        }
    }
}
