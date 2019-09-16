using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KinesisSharp.Lease
{
    public interface ILeaseRegistry
    {
        Task<RecoverLeaseResult> RecoverLeaseAsync(string ownerId);
        Task<TakeLeaseResult> TakeLeaseAsync(string leaseId, string owner);
        Task UpdateLeaseAsync(Lease lease);
    }


    public class InMemoryLeaseRegistry : ILeaseRegistry
    {
        private static readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
        private static readonly List<Lease> leases = new List<Lease>();

        public async Task<RecoverLeaseResult> RecoverLeaseAsync(string ownerId)
        {
            try
            {
                await mutex.WaitAsync();
                var lease = leases.FirstOrDefault(l => l.Owner == ownerId);
                return new RecoverLeaseResult(lease != null, lease);
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<TakeLeaseResult> TakeLeaseAsync(string leaseId, string owner)
        {
            try
            {
                await mutex.WaitAsync();
                var lease = leases.FirstOrDefault(l => l.ShardId == leaseId && l.Owner != owner);
                if (lease != null)
                {
                    return new TakeLeaseResult(false, null);
                }

                lease = leases.FirstOrDefault(l => l.ShardId == leaseId);
                if (lease == null)
                {
                    lease = new Lease
                    {
                        Owner = owner,
                        ShardId = leaseId
                    };
                    leases.Add(lease);
                }

                return new TakeLeaseResult(true, lease);
            }
            finally
            {
                mutex.Release();
            }
        }

        public Task UpdateLeaseAsync(Lease lease)
        {
            try
            {
                mutex.WaitAsync();
                for (var i = 0; i < leases.Count; i++)
                {
                    if (leases[i].ShardId == lease.ShardId)
                    {
                        leases[i] = lease;
                    }
                }

                return Task.CompletedTask;
            }
            finally
            {
                mutex.Release();
            }
        }
    }
}