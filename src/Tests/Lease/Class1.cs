using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Lease;
using KinesisSharp.Lease.Registry;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Tests.Mocks;
using Xunit;

namespace Tests.Lease
{
    public class LeaseCoordinationServiceTests
    {
        public LeaseCoordinationServiceTests()
        {
            queryMock = new Mock<ILeaseRegistryQuery>();
        }

        private readonly Mock<ILeaseRegistryQuery> queryMock;


        [Fact]
        public async Task DiscoverNewLeasesOnStartup()
        {
            queryMock.Setup(x => x.GetAllLeasesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadOnlyCollection<KinesisSharp.Lease.Lease>(new List<KinesisSharp.Lease.Lease>()));

            IOptions<ApplicationConfiguration> configuration = new OptionsWrapper<ApplicationConfiguration>(
                new ApplicationConfiguration
                {
                    StreamArn = "reader-stream-1",
                    Position = InitialPositionType.TrimHorizon,
                    ApplicationName = "test-DiscoverNewLeasesOnStartup"
                });

            var subject = new LeaseCoordinationService(configuration, queryMock.Object, new DiscoverShardsMock());

            var response = await subject.ResolveLeasesForShards();


            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shardId-000000000000", "shardId-000000000001", "shardId-000000000002", "shardId-000000000003",
                    "shardId-000000000004"
                }, true);
        }
    }
}