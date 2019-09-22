using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class IntegrationTests : ContainerBasedFixture<IAmazonKinesis>
    {
        public IntegrationTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        private readonly ITestOutputHelper outputHelper;

        protected override IServiceCollection RegisterSubject(IServiceCollection services)
        {
            services.AddLocalStack();
            return services;
        }


        [Fact]
        public async Task Test1()
        {
            var streamName = "reader-stream";

            var response = await Subject.ListShardsAsync(new ListShardsRequest
            {
                StreamName = streamName
            }).ConfigureAwait(false);
            outputHelper.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        }

        [Fact]
        public async Task Test2()
        {
            outputHelper.WriteLine(JsonConvert.SerializeObject(
                await new DiscoverShardsMock().GetShardsAsync("reader-stream-1", CancellationToken.None)
                    .ConfigureAwait(false)));
        }

        [Fact]
        public async Task Test3()
        {
            var shard1 = Shards.Create("shard-1", 0, 10);
            var shard2 = Shards.Create("shard-2", 10, 20);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 20, 50);
            var shard5 = Shards.Create("shard-5", 50, 100);
            var shard6 = Shards.Merge("shard-6", shard4, shard5, 200);
            var shard7 = Shards.Create("shard-7", 100, null);

            var inMemory = new InMemoryDiscoverShards(shard1, shard2, shard3, shard4, shard5, shard6, shard7);

            outputHelper.WriteLine(
                JsonConvert.SerializeObject(await inMemory.GetShardsAsync("test", CancellationToken.None)
                    .ConfigureAwait(false), Formatting.Indented));
        }

        [Fact]
        public async Task Test4()
        {
            var shard1 = Shards.Create("shard-1", 0, 5);
            var shard2 = Shards.Create("shard-2", 5, 10);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 10, null);
            var (shard5, shard6) = Shards.Split(shard3, "150", "shard-5", "shard-6", 6);

            var inMemory = new InMemoryDiscoverShards(shard1, shard2, shard3, shard4, shard5, shard6);

            outputHelper.WriteLine(
                JsonConvert.SerializeObject(await inMemory.GetShardsAsync("test", CancellationToken.None)
                    .ConfigureAwait(false), Formatting.Indented));
        }
    }
}