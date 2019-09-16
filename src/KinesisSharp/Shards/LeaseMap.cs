using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KinesisSharp.Shards
{
    public class LeaseMap : ReadOnlyDictionary<string, Lease.Lease>
    {
        public LeaseMap(IEnumerable<Lease.Lease> leases) : base(leases.ToDictionary(l => l.ShardId, l => l))
        {
        }
    }
}