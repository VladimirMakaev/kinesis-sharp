using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KinesisSharp.Leases;

namespace KinesisSharp.Shards
{
    public class LeaseMap : ReadOnlyDictionary<string, Lease>
    {
        public LeaseMap(IEnumerable<Lease> leases) : base(leases.ToDictionary(l => l.ShardId, l => l))
        {
        }
    }
}