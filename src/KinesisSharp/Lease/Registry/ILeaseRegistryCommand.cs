using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Registry
{
    public interface ILeaseRegistryCommand
    {
        Task<bool> CreateLease(string application, Lease lease, CancellationToken token);

        Task<UpdateLeaseResult> UpdateLease(Lease lease, CancellationToken token);

        Task<bool> DeleteLease(string shardId, CancellationToken token);
    }

}