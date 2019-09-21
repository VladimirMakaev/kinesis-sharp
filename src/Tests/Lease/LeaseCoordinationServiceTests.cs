using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp;
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

        private void SetupCurrentLeases(params KinesisSharp.Lease.Lease[] leases)
        {
            queryMock.Setup(x => x.GetAllLeasesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadOnlyCollection<KinesisSharp.Lease.Lease>(leases?.ToList() ??
                                                                               new List<KinesisSharp.Lease.Lease>()));
        }

        private static IOptions<ApplicationConfiguration> CreateConfiguration()
        {
            IOptions<ApplicationConfiguration> configuration = new OptionsWrapper<ApplicationConfiguration>(
                new ApplicationConfiguration
                {
                    StreamArn = "reader-stream-1",
                    Position = InitialPositionType.TrimHorizon,
                    ApplicationName = "test-DiscoverNewLeasesOnStartup"
                });
            return configuration;
        }

        private static KinesisSharp.Lease.Lease Lease(string shardId, string position)
        {
            return new KinesisSharp.Lease.Lease
            {
                ShardId = shardId,
                Owner = "worker-2",
                Checkpoint = new ShardPosition(position)
            };
        }


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

            var response = await subject.ResolveLeasesForShards().ConfigureAwait(false);


            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shardId-000000000000",
                    "shardId-000000000001",
                    "shardId-000000000002",
                    "shardId-000000000003",
                    "shardId-000000000004"
                }, true);
        }

        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- / \          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //     (0,5]  (5, 10], (10, )

        [Fact]
        public async Task LeasesToBeCreated_LeaseForShard2Exists_LeasesFor1and4Created()
        {
            var shard1 = Shards.Create("shard-1", 0, 5);
            var shard2 = Shards.Create("shard-2", 5, 10);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 10, null);

            SetupCurrentLeases(Lease("shard-2", "30"));

            var configuration = CreateConfiguration();

            var subject = new LeaseCoordinationService(configuration, queryMock.Object,
                new InMemoryDiscoverShards(shard1, shard2, shard3, shard4));

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-1",
                    "shard-4"
                }, true);
        }


        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- / \          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //     (0,5]  (5, 10], (10, )
        [Fact]
        public async Task LeasesToBeCreated_NoPriorLeasesExist_ShardsWithNoParentFirst()
        {
            var shard1 = Shards.Create("shard-1", 0, 5);
            var shard2 = Shards.Create("shard-2", 5, 10);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 10, null);

            SetupCurrentLeases();

            var configuration = CreateConfiguration();

            var subject = new LeaseCoordinationService(configuration, queryMock.Object,
                new InMemoryDiscoverShards(shard1, shard2, shard3, shard4));

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-1",
                    "shard-2",
                    "shard-4"
                }, true);
        }


        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- / \          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //     (0,5]  (5, 10], (10, )
        [Fact]
        public async Task LeasesToBeCreated_Streams1and2Ended_LeasesFor3and4()
        {
            var shard1 = Shards.Create("shard-1", 0, 5);
            var shard2 = Shards.Create("shard-2", 5, 10);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 10, null);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber)
            );

            var configuration = CreateConfiguration();

            var subject = new LeaseCoordinationService(configuration, queryMock.Object,
                new InMemoryDiscoverShards(shard1, shard2, shard3, shard4));

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeCreated.Select(l => l.ShardId).ShouldBe(new[] {"shard-3", "shard-4"}, true);
        }


        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- / \          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //     (0,5]  (5, 10], (10, )
        [Fact]
        public async Task LeasesToBeDeleted_PointerAt150_LeasesFor1And2()
        {
            var shard1 = Shards.Create("shard-1", 0, 5);
            var shard2 = Shards.Create("shard-2", 5, 10);
            var shard3 = Shards.Merge("shard-3", shard1, shard2, 100);
            var shard4 = Shards.Create("shard-4", 10, null);


            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-3", "150"),
                Lease("shard-4", "50")
            );

            var configuration = CreateConfiguration();

            var subject = new LeaseCoordinationService(configuration, queryMock.Object,
                new InMemoryDiscoverShards(shard1, shard2, shard3, shard4));

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeDeleted.Select(l => l.ShardId).ShouldBe(new[] {"shard-1", "shard-2"}, true);
        }
    }
}