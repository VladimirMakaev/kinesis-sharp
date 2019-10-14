using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Common;

namespace KinesisSharp.Leases.Registry
{
    public interface ILeaseRegistryCommand
    {
        Task<bool> CreateLease(string application, Lease lease, CancellationToken token);

        Task<Result<Lease>> AssignToWorker(string application, Lease lease, string worker);

        Task<UpdateLeaseResult> UpdateLease(string application, Lease lease, CancellationToken token);

        Task<bool> DeleteLease(string application, string shardId, CancellationToken token);
    }
}
