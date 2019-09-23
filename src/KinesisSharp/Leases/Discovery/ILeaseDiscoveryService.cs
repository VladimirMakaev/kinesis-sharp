using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Leases.Discovery
{
    public interface ILeaseDiscoveryService
    {
        Task<LeaseMatchingResult> ResolveLeasesForShards(CancellationToken token = default);
    }
}