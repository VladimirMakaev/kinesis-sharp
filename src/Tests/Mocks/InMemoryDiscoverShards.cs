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
        private readonly Shard[] shards;

        public InMemoryDiscoverShards(params Shard[] shards)
        {
            this.shards = shards;
        }

        public Task<IReadOnlyCollection<Shard>> GetShardsAsync(string streamName, CancellationToken token)
        {
            return Task.FromResult((IReadOnlyCollection<Shard>) new ReadOnlyCollection<Shard>(shards.ToList()));
        }
    }
}