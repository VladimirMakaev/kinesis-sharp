using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Leases.Registry
{
    public interface ILeaseRegistryQuery
    {
        Task<IReadOnlyCollection<Lease>> GetAllLeasesAsync(string application, CancellationToken token);

        Task<IReadOnlyCollection<Lease>> GetAssignedLeasesAsync(string application, string workerId, CancellationToken token);
    }
}