using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Shards
{
    public interface IShardEnumerator
    {
        Task<IReadOnlyCollection<ShardRef>> GetShards(CancellationToken token);
    }

    public class ShardEnumerator : IShardEnumerator
    {
        private readonly IAmazonKinesis kinesis;
        private readonly string streamName;

        public ShardEnumerator(string streamName, IAmazonKinesis kinesis)
        {
            this.streamName = streamName;
            this.kinesis = kinesis;
        }

        public async Task<IReadOnlyCollection<ShardRef>> GetShards(CancellationToken token)
        {
            var shards = await kinesis.ListShardsAsync(new ListShardsRequest
            {
                StreamName = streamName
            }, token);

            return new ReadOnlyCollection<ShardRef>(YieldShards(shards).ToList());
        }

        private IEnumerable<ShardRef> YieldShards(ListShardsResponse shards)
        {
            foreach (var shard in shards.Shards)
            {
                yield return new ShardRef(shard.ShardId, null, null);
            }
        }
    }
}