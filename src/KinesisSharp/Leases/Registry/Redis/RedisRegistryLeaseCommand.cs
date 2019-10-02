using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace KinesisSharp.Leases.Registry.Redis
{
    public class RedisRegistryLeaseCommand : ILeaseRegistryCommand
    {
        private readonly IConnectionMultiplexer multiplexer;

        public RedisRegistryLeaseCommand(IConnectionMultiplexer multiplexer)
        {
            this.multiplexer = multiplexer;
        }

        public async Task<bool> CreateLease(string application, Lease lease, CancellationToken token)
        {
            var db = multiplexer.GetDatabase();
            var transaction = db.CreateTransaction();

            var task1 = transaction.StringSetAsync(Keys.Lease(application, lease.ShardId),
                JsonConvert.SerializeObject(lease, Formatting.Indented), null, When.NotExists);
            var task2 = transaction.SetAddAsync(Keys.AllLeases(application), lease.ShardId);

            return await transaction.ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<UpdateLeaseResult> UpdateLease(string application, Lease lease, CancellationToken token)
        {
            var db = multiplexer.GetDatabase();
            await db.StringSetAsync(Keys.Lease(application, lease.ShardId),
                    JsonConvert.SerializeObject(lease, Formatting.Indented), null, When.NotExists)
                .ConfigureAwait(false);
            return UpdateLeaseResult.Success(lease);
        }

        public async Task<bool> DeleteLease(string application, string shardId, CancellationToken token)
        {
            var db = multiplexer.GetDatabase();
            var transaction = db.CreateTransaction();
            await transaction.KeyDeleteAsync(Keys.Lease(application, shardId)).ConfigureAwait(false);
            await transaction.SetRemoveAsync(Keys.AllLeases(application), shardId).ConfigureAwait(false);
            return await transaction.ExecuteAsync().ConfigureAwait(false);
        }
    }
}