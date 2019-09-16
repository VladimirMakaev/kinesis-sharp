using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Registry
{
    public class InMemoryLeaseRegistryQuery : ILeaseRegistryQuery
    {
        public Task<IReadOnlyCollection<Lease>> GetAllLeasesAsync(string application, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Lease>> GetAssignedLeasesAsync(string application, string workerId, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}