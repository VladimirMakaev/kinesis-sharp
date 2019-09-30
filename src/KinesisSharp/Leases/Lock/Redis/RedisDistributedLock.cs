﻿using System;
using System.Threading.Tasks;
using KinesisSharp.Common;
using StackExchange.Redis;

namespace KinesisSharp.Leases.Lock.Redis
{
    public class RedisDistributedLock : IDistributedLockService
    {
        private readonly IConnectionMultiplexer multiplexer;

        public RedisDistributedLock(IConnectionMultiplexer multiplexer)
        {
            this.multiplexer = multiplexer;
        }

        public async Task<LockResult> LockResource(string resourceName, string ownerId, TimeSpan duration)
        {
            var db = multiplexer.GetDatabase();
            var lockId = Keys.Lock(resourceName);
            var result = await db.LockTakeAsync(lockId, ownerId, duration).ConfigureAwait(false);
            if (result)
            {
                return LockResult.Success(new Lock(lockId, resourceName, TimerProvider.UtcNow + duration, ownerId));
            }

            return LockResult.Fail(LockError.AlreadyLocked);
        }

        public async Task<LockResult> ExtendLock(Lock lockObject, TimeSpan duration)
        {
            var db = multiplexer.GetDatabase();
            var result = await db.LockExtendAsync(lockObject.LockId, lockObject.OwnerId, duration)
                .ConfigureAwait(false);

            if (result)
            {
                return LockResult.Success(new Lock(lockObject.LockId, lockObject.Resource,
                    TimerProvider.UtcNow + duration, lockObject.OwnerId));
            }

            return LockResult.Fail(LockError.AlreadyLocked);
        }

        public async Task UnlockResource(Lock lockObject)
        {
            var db = multiplexer.GetDatabase();
            await db.LockReleaseAsync(lockObject.LockId, lockObject.OwnerId).ConfigureAwait(false);
        }

        private static class Keys
        {
            public static string Lock(string resource)
            {
                return $"L:{resource}";
            }
        }
    }
}
