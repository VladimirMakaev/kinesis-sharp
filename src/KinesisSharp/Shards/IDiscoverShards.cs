using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Shards
{
    public interface IDiscoverShards
    {
        Task<IReadOnlyCollection<Shard>> GetShardsAsync(string streamName, CancellationToken token);
    }

    public class DiscoverShards : IDiscoverShards
    {
        private const int BatchSize = 2;
        private readonly IAmazonKinesis kinesis;

        public DiscoverShards(IAmazonKinesis kinesis)
        {
            this.kinesis = kinesis;
        }

        public async Task<IReadOnlyCollection<Shard>> GetShardsAsync(string streamName, CancellationToken token)
        {
            string tokenRequestToken = null;

            var result = new List<Shard>();
            do
            {
                var response = await kinesis.ListShardsAsync(new ListShardsRequest
                {
                    StreamName = streamName,
                    MaxResults = BatchSize
                }, token).ConfigureAwait(false);

                result.AddRange(response.Shards);
                tokenRequestToken = response.NextToken;
            } while (tokenRequestToken != null);


            return new ReadOnlyCollection<Shard>(result);
        }
    }
}