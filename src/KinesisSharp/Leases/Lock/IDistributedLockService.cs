using System;
using System.Threading.Tasks;
using KinesisSharp.Common;

namespace KinesisSharp.Leases.Lock
{
    public interface IDistributedLockService
    {
        Task<Result<Lock>> LockResource(string resourceName, string ownerId, TimeSpan duration);

        Task<Result<Lock>> ExtendLock(Lock lockObject, TimeSpan duration);

        Task UnlockResource(Lock lockObject);
    }
}
