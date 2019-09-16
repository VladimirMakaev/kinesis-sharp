using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KinesisSharp.Shards
{
    public class Coordinator : BackgroundService
    {
        private readonly IDiscoverShards discoverShards;
        private readonly IOptions<StreamConfiguration> streamConfiguration;

        public Coordinator(IOptions<StreamConfiguration> streamConfiguration, IDiscoverShards discoverShards)
        {
            this.streamConfiguration = streamConfiguration;
            this.discoverShards = discoverShards;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var shards = await discoverShards.GetShardsAsync(streamConfiguration.Value.StreamArn, stoppingToken);


        }
    }
}