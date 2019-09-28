using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Leases.Discovery;
using KinesisSharp.Leases.Registry;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Tests.Mocks;
using Xunit;

namespace Tests.Lease
{
    public class LeaseDiscoveryServiceTests
    {
        public LeaseDiscoveryServiceTests()
        {
            queryMock = new Mock<ILeaseRegistryQuery>();
            shards = new MockShardCollection();
        }

        private readonly Mock<ILeaseRegistryQuery> queryMock;
        private readonly MockShardCollection shards;

        private void SetupCurrentLeases(params KinesisSharp.Leases.Lease[] leases)
        {
            queryMock.Setup(x => x.GetAllLeasesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReadOnlyCollection<KinesisSharp.Leases.Lease>(leases?.ToList() ??
                                                                                new List<KinesisSharp.Leases.Lease>()));
        }

        private static IOptions<ApplicationConfiguration> CreateConfiguration(
            InitialPositionType initialPositionType = InitialPositionType.TrimHorizon)
        {
            IOptions<ApplicationConfiguration> configuration = new OptionsWrapper<ApplicationConfiguration>(
                new ApplicationConfiguration
                {
                    StreamArn = "reader-stream-1",
                    Position = initialPositionType,
                    ApplicationName = "test-DiscoverNewLeasesOnStartup"
                });
            return configuration;
        }

        private static KinesisSharp.Leases.Lease Lease(string shardId, string position)
        {
            return new KinesisSharp.Leases.Lease
            {
                ShardId = shardId,
                Owner = "worker-1",
                Checkpoint = new ShardPosition(position)
            };
        }

        private LeaseDiscoveryService CreateSubject()
        {
            var configuration = CreateConfiguration();

            var subject =
                new LeaseDiscoveryService(configuration, queryMock.Object, new InMemoryDiscoverShards(shards));
            return subject;
        }


        //     Shard setup is the following (bottom to top):
        //         
        //       |              |            |
        //       |              |            |
        //300   /9\             |            |
        //     /   \            |            |
        //    |     |           |            |
        //    |     |           |            |
        //200 |     |          /8\           |
        //    |     |         /   \          |
        //    |     |        /     \         |
        //    |     |       |       |        |
        //    |     |       |       |        |
        //100 |     |      /7\      |        |
        //    |     |     /   \     |        |
        //    |     |    |     |    |        |
        //    |     |    |     |    |        |
        //    1     2    3     4    5        6
        [Fact]
        public async Task LeasesToBeCreated_BrokenShardPresent_BrokenShardSkipped()
        {
            shards.Create("shard-1", 0, 10); //broken
            shards.Create("shard-2", 10, 20);
            shards.Create("shard-3", 30, 40);
            shards.Create("shard-4", 50, 60);
            shards.Create("shard-5", 60, 70);
            shards.Create("shard-6", 70, 80);
            shards.Merge("shard-7", "shard-3", "shard-4", 100);
            shards.Merge("shard-8", "shard-7", "shard-5", 200);
            shards.Merge("shard-9", "shard-1", "shard-2", 300);
            shards.RemoveAt(0); //Make shard broken

            SetupCurrentLeases();

            var subject = CreateSubject();

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-2",
                    "shard-3",
                    "shard-4",
                    "shard-5",
                    "shard-6"
                }, true);
        }


        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- /3\          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //        1     2        4
        [Fact]
        public async Task LeasesToBeCreated_LeaseForShard2Exists_LeasesFor1and4Created()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);

            SetupCurrentLeases(Lease("shard-2", "30"));

            var subject = CreateSubject();

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-1",
                    "shard-4"
                }, true);
        }


        //     Shard setup is the following (bottom to top):
        //         
        //       |              |            |
        //       |              |            |
        //300   /9\             |            |
        //     /   \            |            |
        //    |     |           |            |
        //    |     |           |            |
        //200 |     |          /8\           |
        //    |     |         /   \          |
        //    |     |        /     \         |
        //    |     |       |       |        |
        //    |     |       |       |        |
        //100 |     |      /7\      |        |
        //    |     |     /   \     |        |
        //    |     |    |     |    |        |
        //    |     |    |     |    |        |
        //    1     2    3     4    5        6
        [Fact]
        public async Task LeasesToBeCreated_MultiHierarchy_LeafNodesFirst()
        {
            shards.Create("shard-1", 0, 10);
            shards.Create("shard-2", 10, 20);
            shards.Create("shard-3", 30, 40);
            shards.Create("shard-4", 50, 60);
            shards.Create("shard-5", 60, 70);
            shards.Create("shard-6", 70, 80);
            shards.Merge("shard-7", "shard-3", "shard-4", 100);
            shards.Merge("shard-8", "shard-7", "shard-5", 200);
            shards.Merge("shard-9", "shard-1", "shard-2", 300);

            SetupCurrentLeases();

            var subject = CreateSubject();

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-1",
                    "shard-2",
                    "shard-3",
                    "shard-4",
                    "shard-5",
                    "shard-6"
                }, true);
        }


        //     Shard setup is the following (bottom to top):
        //           |           |
        //     100- /3\          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //        1     2        4
        [Fact]
        public async Task LeasesToBeCreated_NoPriorLeasesExist_ShardsWithNoParentFirst()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);

            SetupCurrentLeases();

            var subject = CreateSubject();

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
        //     100- /3\          |
        //         /   \         |
        //        |     |        |
        //        |     |        |
        //        1     2        4
        [Fact]
        public async Task LeasesToBeCreated_Streams1and2Ended_LeasesFor3and4()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber)
            );

            var subject = CreateSubject();

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeCreated.Select(l => l.ShardId).ShouldBe(new[] {"shard-3", "shard-4"}, true);
        }


        //     Shard setup is the following (bottom to top):
        //         \   /         |  
        //         5\ /6         |
        //    150-   |           |
        //           |           |
        //    100-  /3\          |
        //         /   \         |
        //        |     |        |
        //        1     2        4  
        [Fact]
        public async Task LeasesToBeCreated_Streams1Ended_LeasesFor2and4()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);
            shards.Split("shard-3", "150", "shard-5", "shard-6", 6);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber)
            );

            var subject = CreateSubject();

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeCreated.Select(l => l.ShardId).ShouldBe(new[] {"shard-2", "shard-4"}, true);
        }


        //     Shard setup is the following (bottom to top):
        //         
        //       |              |            |
        //       |              |            |
        //300   /9\             |            |
        //     /   \            |            |
        //    |     |           |            |
        //    |     |           |            |
        //200 |     |          /8\           |
        //    |     |         /   \          |
        //    |     |        /     \         |
        //    |     |       |       |        |
        //    |     |       |       |        |
        //100 |     |      /7\      |        |
        //    |     |     /   \     |        |
        //    |     |    |     |    |        |
        //    |     |    |     |    |        |
        //    1     2    3     4    5        6
        [Fact]
        public async Task LeasesToBeDeleted_LeasesAreRead_ShouldBeDeleted()
        {
            shards.Create("shard-1", 0, 10);
            shards.Create("shard-2", 10, 20);
            shards.Create("shard-3", 30, 40);
            shards.Create("shard-4", 50, 60);
            shards.Create("shard-5", 60, 70);
            shards.Create("shard-6", 70, 80);
            shards.Merge("shard-7", "shard-3", "shard-4", 100);
            shards.Merge("shard-8", "shard-7", "shard-5", 200);
            shards.Merge("shard-9", "shard-1", "shard-2", 300);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-3", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-4", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-5", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-6", ShardPosition.ShardEnd.SequenceNumber)
            );

            var subject = CreateSubject();

            var response = await subject.ResolveLeasesForShards();

            response.LeasesToBeCreated.Select(s => s.ShardId).ShouldBe(
                new[]
                {
                    "shard-7",
                    "shard-9"
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
        public async Task LeasesToBeDeleted_PointerAt150_LeasesFor1And2()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-3", "150"),
                Lease("shard-4", "50")
            );

            var subject = CreateSubject();

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeDeleted.Select(l => l.ShardId).ShouldBe(new[] {"shard-1", "shard-2"}, true);
        }


        //     Shard setup is the following (bottom to top):
        //         \   /         |  
        //         5\ /6         |
        //    150-   |           |
        //           |           |
        //    100-  /3\          |
        //         /   \         |
        //        |     |        |
        //        1     2        4  
        [Fact]
        public async Task LeasesToBeDeleted_PointerAt200_LeasesFor123()
        {
            shards.Create("shard-1", 0, 5);
            shards.Create("shard-2", 5, 10);
            shards.Merge("shard-3", "shard-1", "shard-2", 100);
            shards.Create("shard-4", 10, null);
            shards.Split("shard-3", "150", "shard-5", "shard-6", 6);

            SetupCurrentLeases(
                Lease("shard-1", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-2", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-3", ShardPosition.ShardEnd.SequenceNumber),
                Lease("shard-5", "200"),
                Lease("shard-6", "200"),
                Lease("shard-4", "50")
            );

            var subject = CreateSubject();

            var result = await subject.ResolveLeasesForShards().ConfigureAwait(false);

            result.LeasesToBeDeleted.Select(l => l.ShardId).ShouldBe(new[] {"shard-1", "shard-2", "shard-3"}, true);
        }
    }
}