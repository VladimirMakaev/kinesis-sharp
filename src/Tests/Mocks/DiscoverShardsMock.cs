using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;
using KinesisSharp.Shards;
using Newtonsoft.Json;

namespace Tests.Mocks
{
    public class DiscoverShardsMock : IDiscoverShards
    {
        public Task<IReadOnlyCollection<Shard>> GetShardsAsync(string streamName, CancellationToken token)
        {
            return Task.FromResult(
                (IReadOnlyCollection<Shard>) new ReadOnlyCollection<Shard>(GetStringFromResource(streamName)));
        }

        private List<Shard> GetStringFromResource(string resourceName)
        {
            var resourceStream =
                typeof(DiscoverShardsMock).Assembly.GetManifestResourceStream(
                    $"{typeof(DiscoverShardsMock).Namespace}.Samples.{resourceName}.json");
            using (var reader = new StreamReader(resourceStream))
            {
                return JsonConvert.DeserializeObject<List<Shard>>(reader.ReadToEnd());
            }
        }
    }
}