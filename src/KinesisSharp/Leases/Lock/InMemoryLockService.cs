using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using KinesisSharp.Common;

namespace KinesisSharp.Leases.Lock
{
    public class InMemoryLockService : IDistributedLockService
    {
        private readonly ConcurrentDictionary<string, (string Owner, Lock Lock)> locks =
            new ConcurrentDictionary<string, (string, Lock)>();

        public Task<Result<Lock>> LockResource(string resourceName, string ownerId, TimeSpan duration)
        {
            var newLock = new Lock(Guid.NewGuid().ToString("N"),
                resourceName, TimerProvider.UtcNow + duration, ownerId);

            if (locks.TryGetValue(resourceName, out var currentLock))
            {
                if (currentLock.Lock.ExpiresOn <= TimerProvider.UtcNow)
                {
                    if (locks.TryUpdate(resourceName, (ownerId, newLock), currentLock))
                    {
                        return Task.FromResult(Result.Success(newLock));
                    }

                    return Task.FromResult(Result.Fail<Lock>(Errors.AlreadyLocked));
                }

                return Task.FromResult(Result.Fail<Lock>(Errors.AlreadyLocked));
            }

            if (locks.TryAdd(resourceName, (ownerId, newLock)))
            {
                return Task.FromResult(Result.Success(newLock));
            }

            return Task.FromResult(Result.Fail<Lock>(Errors.AlreadyLocked));
        }

        public Task UnlockResource(Lock lockObject)
        {
            if (locks.TryGetValue(lockObject.Resource, out var currentLock))
            {
                if (currentLock.Lock.LockId == lockObject.LockId)
                {
                    locks.TryRemove(lockObject.Resource, out _);
                }
            }

            return Task.CompletedTask;
        }

        public Task<Result<Lock>> ExtendLock(Lock lockObject, TimeSpan duration)
        {
            if (locks.TryGetValue(lockObject.Resource, out var currentLock))
            {
                if (currentLock.Lock.LockId != lockObject.LockId)
                {
                    return Task.FromResult(Result.Fail<Lock>(Errors.AlreadyLocked));
                }

                var newLock = new Lock(lockObject.LockId, lockObject.Resource, TimerProvider.UtcNow + duration,
                    lockObject.OwnerId);
                if (locks.TryUpdate(lockObject.Resource, (currentLock.Owner, newLock), currentLock))
                {
                    return Task.FromResult(Result.Success(newLock));
                }

                return Task.FromResult(Result.Fail<Lock>(Errors.AlreadyLocked));
            }

            return Task.FromResult(Result.Fail<Lock>(Errors.LockNotFound));
        }
    }
}
