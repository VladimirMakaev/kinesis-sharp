using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;
using KinesisSharp.Shards;

namespace Tests.Mocks
{
    public class InMemoryDiscoverShards : IDiscoverShards
    {
        private readonly IEnumerable<Shard> shards;

        public InMemoryDiscoverShards(params Shard[] shards) : this((IEnumerable<Shard>) shards)
        {
        }

        public InMemoryDiscoverShards(IEnumerable<Shard> shards)
        {
            this.shards = shards;
        }

        public Task<IReadOnlyCollection<Shard>> GetShardsAsync(string streamName, CancellationToken token)
        {
            return Task.FromResult((IReadOnlyCollection<Shard>) new ReadOnlyCollection<Shard>(shards.ToList()));
        }
    }
}