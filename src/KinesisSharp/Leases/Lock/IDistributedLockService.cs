using System;
using System.Threading.Tasks;

namespace KinesisSharp.Leases.Lock
{
    public interface IDistributedLockService
    {
        Task<LockResult> LockResource(string resourceName, string ownerId, TimeSpan duration);

        Task<LockResult> ExtendLock(Lock lockObject, TimeSpan duration);

        Task UnlockResource(Lock lockObject);
    }
}
