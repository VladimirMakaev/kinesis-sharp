using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Runtime;
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
            return services.AddSingleton<IAmazonKinesis>(p =>
                new AmazonKinesisClient(
                    new SessionAWSCredentials("ASIA56ZSXAX76G4AIC25", "swsDv8/+dyjfQ+64JaaVmPwoEpJCte+ofDjT/YdE",
                        "FQoGZXIvYXdzENP//////////wEaDETedW0XTS10ETa48CKRAgCgPHgqKgLvPTCDNhB5fvdZ2W/lGUCu2Pobfqyjp3rTUkhKYUH4w5JD7HJqyEPdWMPaCpltU1VK3lK/kliDrktKxiABmfv5rypbSKxZPv+bV1SMVDcsokM3a29TjwaREhAEFUzxtxdPzdcdlyX2o+CKgcgaV8ilnO4qyDNzLdBH1pEVUZhCv8FBsfYuVPFkFPwL0faxMs1vEWM0tTWg9QfFk0ioGQbwoeG+t8NYyNmLBsY7ksKacJgrfcJI0IW+wN6SGKfWyTaxLEnUNFNDVKHUpfX+1RWiXwX9HxuTZCY2V6QU76Tu+Pgj2nYSchmrBWS/cqDZGcYGDHpFRKkyUJYe4iLjHnkh/l7R08IzdZOYNCjGisDsBQ==")
                    , RegionEndpoint.EUWest1));
        }

        [Fact]
        public async Task Test1()
        {
            var shards = await Subject.GetShardsAsync("vladimir-stream-1", CancellationToken.None).ConfigureAwait(false);
            outputHelper.WriteLine(JsonConvert.SerializeObject(shards, Formatting.Indented));
        }
    }
}
