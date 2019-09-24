using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Leases;
using KinesisSharp.Leases.Lock;
using KinesisSharp.Leases.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Lease
{
    public class LeaseClaimingServiceTests : ContainerBasedFixture<LeaseClaimingService>, IDisposable
    {
        public LeaseClaimingServiceTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            leaseRegistryQueryMock = new Mock<ILeaseRegistryQuery>();
            leaseRegistryCommandMock = new Mock<ILeaseRegistryCommand>();
            leaseRegistryCommandMock
                .Setup(x => x.UpdateLease(It.IsAny<string>(), It.IsAny<KinesisSharp.Leases.Lease>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string x1, KinesisSharp.Leases.Lease x2, CancellationToken x3) =>
                    UpdateLeaseResult.Success(x2));
            currentDateTimeNow = new DateTime(2016, 1, 1);
            timeProvider = new Mock<ITimeProvider>();
            timeProvider.Setup(x => x.UtcNow).Returns(() => currentDateTimeNow);
            TimerProvider.SetProvider(timeProvider.Object);
        }

        public void Dispose()
        {
            TimerProvider.SetProvider(new SystemTimeProvider());
        }

        private readonly ITestOutputHelper outputHelper;
        private readonly Mock<ILeaseRegistryQuery> leaseRegistryQueryMock;
        private readonly Mock<ILeaseRegistryCommand> leaseRegistryCommandMock;
        private int maxLeasesPerClaim = 2;
        private int maxLeasesPerWorker = 5;
        private DateTime currentDateTimeNow;
        private readonly Mock<ITimeProvider> timeProvider;

        protected override IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Logging:LogLevel:Default", "Debug"}
            });
        }

        protected override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton(sp => leaseRegistryCommandMock.Object);
            services.AddSingleton(sp => leaseRegistryQueryMock.Object);
            services.AddSingleton<IDistributedLockService, InMemoryLockService>();
            services.Configure<WorkerConfiguration>(config =>
            {
                config.MaxLeasesPerClaim = maxLeasesPerClaim;
                config.MaxLeasesPerWorker = maxLeasesPerWorker;
            });
            return base.ConfigureServices(services);
        }

        protected override IServiceCollection RegisterSubject(IServiceCollection services)
        {
            return base.RegisterSubject(services);
        }

        private void SetupLeases(params KinesisSharp.Leases.Lease[] leases)
        {
            leaseRegistryQueryMock
                .Setup(x => x.GetAllLeasesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<KinesisSharp.Leases.Lease>(leases));
        }

        private static KinesisSharp.Leases.Lease Lease(string shardId, string position, string owner = null,
            DateTime? expiresOn = null)
        {
            return new KinesisSharp.Leases.Lease
            {
                ShardId = shardId,
                Owner = owner,
                Checkpoint = new ShardPosition(position),
                LockExpiresOn = expiresOn
            };
        }

        [Fact]
        public async Task ClaimLeases_1LeaseAvailableNonOtherTaken_LeaseShouldBeTaken()
        {
            SetupLeases(Lease("shard-1", ShardPosition.TrimHorizon.SequenceNumber));
            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);
            result.Select(x => x.Owner).ShouldBe(new[] {"worker-1"});
        }

        [Fact]
        public async Task ClaimLeases_3Workers10Leases_ShouldLoadBalance()
        {
            maxLeasesPerClaim = 5;
            maxLeasesPerWorker = 5;

            SetupLeases(
                Lease("shard-1", ShardPosition.TrimHorizon.SequenceNumber, "worker-1"),
                Lease("shard-2", ShardPosition.TrimHorizon.SequenceNumber, "worker-2"),
                Lease("shard-3", ShardPosition.TrimHorizon.SequenceNumber, "worker-3"),
                Lease("shard-4", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-5", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-6", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-7", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-8", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-9", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-10", ShardPosition.TrimHorizon.SequenceNumber)
            );

            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);
            result.Select(x => x.Owner).ShouldBe(new[] {"worker-1", "worker-1", "worker-1"});
        }

        [Fact]
        public async Task ClaimLeases_ExpiredLeases_ExpiredLeasesShouldbeTaken()
        {
            currentDateTimeNow = new DateTime(2019, 09, 24, 10, 00, 00);
            var expiredTime = currentDateTimeNow - TimeSpan.FromSeconds(5);

            SetupLeases(
                Lease("shard-1", ShardPosition.TrimHorizon.SequenceNumber, "worker-2"),
                Lease("shard-2", ShardPosition.TrimHorizon.SequenceNumber, "worker-2", expiredTime),
                Lease("shard-3", ShardPosition.TrimHorizon.SequenceNumber, "worker-3")
            );

            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);

            result.Select(x => x.ShardId).ShouldBe(new[] {"shard-2"}, true);
        }

        [Fact]
        public async Task ClaimLeases_ExpiredLeasesOwnedByCurrentWorker_ExpiredLeasesShouldBeTaken()
        {
            maxLeasesPerClaim = 3;
            maxLeasesPerWorker = 3;

            currentDateTimeNow = new DateTime(2019, 09, 24, 10, 00, 00);
            var expiredTime = currentDateTimeNow - TimeSpan.FromSeconds(5);

            SetupLeases(
                Lease("shard-1", ShardPosition.TrimHorizon.SequenceNumber, "worker-1", expiredTime),
                Lease("shard-2", ShardPosition.TrimHorizon.SequenceNumber, "worker-1", expiredTime),
                Lease("shard-3", ShardPosition.TrimHorizon.SequenceNumber, "worker-1", expiredTime),
                Lease("shard-4", ShardPosition.TrimHorizon.SequenceNumber),
                Lease("shard-5", ShardPosition.TrimHorizon.SequenceNumber)
            );

            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);

            result.Count.ShouldBe(3);
        }

        [Fact]
        public async Task ClaimLeases_ManyLeasesAvailableNoneTaken_ShouldTakeMaxLeasesPerClaim()
        {
            maxLeasesPerClaim = 5;
            SetupLeases(Enumerable.Range(0, 10)
                .Select(i => Lease($"shard-{i}", ShardPosition.TrimHorizon.SequenceNumber)).ToArray());
            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);
            result.Select(x => x.Owner).ShouldBe(new[] {"worker-1", "worker-1", "worker-1", "worker-1", "worker-1"});
        }


        [Fact]
        public async Task ClaimLeases_WorkerHas4Leases6AreAvailable_ShouldOnlyTakeOne()
        {
            maxLeasesPerClaim = 5;
            maxLeasesPerWorker = 5;

            SetupLeases(Enumerable.Range(0, 10)
                .Select(i => Lease($"shard-{i}", ShardPosition.TrimHorizon.SequenceNumber, i > 5 ? "worker-1" : null))
                .ToArray()
            );

            var result = await Subject.ClaimLeases("test", "worker-1").ConfigureAwait(false);
            result.Select(x => x.Owner).ShouldBe(new[] {"worker-1"});
        }
    }
}