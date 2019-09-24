using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Leases.Lock;
using KinesisSharp.Leases.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp.Leases
{
    public interface ILeaseClaimingService
    {
        Task<IReadOnlyList<Lease>> ClaimLeases(string application, string workerId,
            CancellationToken token = default);
    }

    public class LeaseClaimingService : ILeaseClaimingService
    {
        private readonly IOptions<WorkerConfiguration> configuration;
        private readonly ILeaseRegistryCommand leaseCommand;
        private readonly ILeaseRegistryQuery leaseQuery;
        private readonly IDistributedLockService lockService;
        private readonly ILogger<LeaseClaimingService> logger;

        public LeaseClaimingService(ILogger<LeaseClaimingService> logger, IOptions<WorkerConfiguration> configuration,
            ILeaseRegistryQuery leaseQuery, ILeaseRegistryCommand leaseCommand, IDistributedLockService lockService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.leaseQuery = leaseQuery;
            this.leaseCommand = leaseCommand;
            this.lockService = lockService;
        }

        public async Task<IReadOnlyList<Lease>> ClaimLeases(string application, string workerId,
            CancellationToken token = default)
        {
            var allLeases = await leaseQuery.GetAllLeasesAsync(application, token).ConfigureAwait(false);
            var (knownWorkers, leasesTakenByAllWorkers, leasesTakenByCurrentWorker) =
                GetStatsForAllLeases(workerId, allLeases);
            var target = allLeases.Count / knownWorkers + (allLeases.Count % knownWorkers == 0 ? 0 : 1);
            var maxTarget = Math.Min(target, configuration.Value.MaxLeasesPerWorker);
            var numberOfLeasesToTake =
                Math.Min(maxTarget - leasesTakenByCurrentWorker, configuration.Value.MaxLeasesPerClaim);
            var leasesTaken = 0;
            var result = new List<Lease>(numberOfLeasesToTake);
            foreach (var lease in Utility.Shuffle(AvailableLeasesByPriority(allLeases), new Random()))
            {
                var @lock = await lockService.LockResource(lease.ShardId, workerId,
                    configuration.Value.LeaseLockDuration).ConfigureAwait(false);

                if (@lock.IsSuccess)
                {
                    lease.Owner = workerId;
                    lease.LockExpiresOn = @lock.Lock.ExpiresOn;
                    var updateLeaseResult = await leaseCommand.UpdateLease(application, lease, token)
                        .ConfigureAwait(false);

                    if (updateLeaseResult.IsSuccess)
                    {
                        result.Add(lease);
                        leasesTaken++;
                        if (leasesTaken >= numberOfLeasesToTake)
                        {
                            break;
                        }
                    }
                }
            }

            return new ReadOnlyCollection<Lease>(result);
        }

        private IEnumerable<Lease> AvailableLeasesByPriority(IReadOnlyCollection<Lease> allLeases)
        {
            return allLeases
                .Where(l => l.Owner == null || l.LockExpiresOn != null && l.LockExpiresOn < TimerProvider.UtcNow);
        }

        private (int KnownWorkers, int LeasesTakenByAllWorkers, int CurrentWorkerLeases) GetStatsForAllLeases(
            string workerId,
            IEnumerable<Lease> leases)
        {
            var rest = leases.GroupBy(l => l.Owner)
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Expired = g.Count(x => x.LockExpiresOn != null && x.LockExpiresOn <= TimerProvider.UtcNow)
                })
                .Aggregate(
                    (KnownWorkers: 1, LeasesTakenByAllWorkers: 0, CurrentWorkerLeases: 0),
                    (s, y) => (
                        s.KnownWorkers + (y.Key == workerId || y.Key == null ? 0 : 1),
                        s.LeasesTakenByAllWorkers + (y.Key == null ? 0 : y.Count) - y.Expired,
                        s.CurrentWorkerLeases = y.Key == workerId ? y.Count - y.Expired : s.CurrentWorkerLeases
                    )
                );

            return rest;
        }
    }
}