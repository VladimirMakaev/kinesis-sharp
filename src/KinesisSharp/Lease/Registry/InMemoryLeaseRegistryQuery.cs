using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Registry
{
    public class InMemoryLeaseRegistryQuery : ILeaseRegistryQuery
    {
        private readonly IDictionary<string, List<Lease>> sharedMemory;

        public InMemoryLeaseRegistryQuery(IDictionary<string, List<Lease>> sharedMemory)
        {
            this.sharedMemory = sharedMemory;
        }

        public Task<IReadOnlyCollection<Lease>> GetAllLeasesAsync(string application, CancellationToken token)
        {
            var allLeases = sharedMemory[application];
            return Task.FromResult(
                (IReadOnlyCollection<Lease>) new ReadOnlyCollection<Lease>(allLeases));
        }

        public async Task<IReadOnlyCollection<Lease>> GetAssignedLeasesAsync(string application, string workerId,
            CancellationToken token)
        {
            var allLeases = sharedMemory[application];
            var workerLeases = allLeases.Where(l => l.Owner == workerId).ToList();
            return await Task.FromResult(new ReadOnlyCollection<Lease>(workerLeases)).ConfigureAwait(false);
        }
    }
}