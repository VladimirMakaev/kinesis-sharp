using System.Linq;
using System.Threading.Tasks;
using KinesisSharp;
using KinesisSharp.Records;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Mocks
{
    public class StubKinesisShardReaderFactoryTests
    {
        public StubKinesisShardReaderFactoryTests(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        private readonly ITestOutputHelper helper;

        [Fact]
        public async Task Test1()
        {
            var factory = new StubKinesisShardReaderFactory(10000, 100);

            var result = await factory.CreateReaderAsync(new ShardRef("shard-1", null), ShardPosition.TrimHorizon)
                .ConfigureAwait(false);

            while (!result.EndOfShard)
            {
                await result.ReadNextAsync().ConfigureAwait(false);
                helper.WriteLine(result.Records.Count.ToString());
            }
        }
    }
}