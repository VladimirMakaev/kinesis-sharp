using System;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Registry
{
    public interface IDistributedLockService
    {
        Task LockResource(string resourceName, string ownerId, TimeSpan duration);

        Task ExtendLock(string resourceName, TimeSpan duration);

        Task UnlockResource(string lockId);
    }
}