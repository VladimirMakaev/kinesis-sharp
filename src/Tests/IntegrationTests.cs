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
                await new DiscoverShardsMock().GetShardsAsync("reader-stream-1", CancellationToken.None).ConfigureAwait(false)));
        }
    }
}