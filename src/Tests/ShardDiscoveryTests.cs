using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Shards;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ShardDiscoveryTests : ContainerBasedFixture<DiscoverShards>
    {
        public ShardDiscoveryTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        private readonly ITestOutputHelper outputHelper;

        protected override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            return services.AddLocalStack();
        }

        [Fact]
        public async Task Test1()
        {
            var shards = await Subject.GetShardsAsync("reader-stream", CancellationToken.None).ConfigureAwait(false);
            outputHelper.WriteLine(JsonConvert.SerializeObject(shards, Formatting.Indented));
        }
    }
}