using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Common;

namespace KinesisSharp.Leases.Registry
{
    public class InMemoryLeaseRegistry : ILeaseRegistryQuery, ILeaseRegistryCommand
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Lease>> sharedMemory =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Lease>>();

        public Task<bool> CreateLease(string application, Lease lease, CancellationToken token)
        {
            var leases = sharedMemory.GetOrAdd(application, new ConcurrentDictionary<string, Lease>());
            return Task.FromResult(leases.TryAdd(lease.ShardId, lease));
        }

        public Task<bool> DeleteLease(string application, string shardId, CancellationToken token)
        {
            var leases = sharedMemory.GetOrAdd(application, new ConcurrentDictionary<string, Lease>());
            return Task.FromResult(leases.TryRemove(shardId, out _));
        }

        public async Task<UpdateLeaseResult> UpdateLease(string application, Lease lease, CancellationToken token)
        {
            var leases = sharedMemory.GetOrAdd(application, new ConcurrentDictionary<string, Lease>());
            if (leases.TryGetValue(lease.ShardId, out var currentLease))
            {
                if (leases.TryUpdate(lease.ShardId, lease, currentLease))
                {
                    return await Task.FromResult(UpdateLeaseResult.Success(lease))
                        .ConfigureAwait(false);
                }

                return UpdateLeaseResult.Fail(LeaseRegistryOperationError.StaleData);
            }

            return UpdateLeaseResult.Fail(LeaseRegistryOperationError.NotFound);
        }


        public async Task<IReadOnlyCollection<Lease>> GetAllLeasesAsync(string application, CancellationToken token)
        {
            if (sharedMemory.TryGetValue(application, out var allLeases))
            {
                return new ReadOnlyCollection<Lease>(await Task.FromResult(allLeases.Values.ToList())
                    .ConfigureAwait(false));
            }

            return Empty<Lease>.ReadOnlyList;
        }


        public async Task<IReadOnlyCollection<Lease>> GetAssignedLeasesAsync(string application, string workerId,
            CancellationToken token)
        {
            if (sharedMemory.TryGetValue(application, out var allLeases))
            {
                var workerLeases = allLeases.Values.Where(l => l.Owner == workerId && !l.Checkpoint.IsEnded).ToList();
                return await Task.FromResult(new ReadOnlyCollection<Lease>(workerLeases)).ConfigureAwait(false);
            }

            return Empty<Lease>.ReadOnlyList;
        }
    }
}