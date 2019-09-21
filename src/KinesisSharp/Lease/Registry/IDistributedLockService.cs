using System;
using System.Threading.Tasks;

namespace KinesisSharp.Lease.Registry
{
    public interface IDistributedLockService
    {
        Task<LockResult> LockResource(string resourceName, string ownerId, TimeSpan duration);

        Task<LockResult> ExtendLock(Lock lockObject, TimeSpan duration);

        Task UnlockResource(Lock lockObject);
    }

    public class Lock
    {
        public DateTime ExpiresOn { get; set; }
        public string LockId { get; set; }
    }


    public class LockResult
    {
        public bool Success { get; set; }

        public Lock Lock { get; set; }
    }
}