using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Discovery
{
    public interface ILeaseDiscoveryService
    {
        Task<LeaseMatchingResult> ResolveLeasesForShards(CancellationToken token = default);
    }
}