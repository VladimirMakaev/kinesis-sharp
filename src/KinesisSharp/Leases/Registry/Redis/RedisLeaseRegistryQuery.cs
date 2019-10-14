using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace KinesisSharp.Leases.Registry.Redis
{
    public class RedisLeaseRegistryQuery : ILeaseRegistryQuery
    {
        private readonly IConnectionMultiplexer multiplexer;

        public RedisLeaseRegistryQuery(IConnectionMultiplexer multiplexer)
        {
            this.multiplexer = multiplexer;
        }

        public async Task<IReadOnlyCollection<Lease>> GetAllLeasesAsync(string application, CancellationToken token)
        {
            var db = multiplexer.GetDatabase();
            var members = await db.SetMembersAsync(Keys.AllLeases(application)).ConfigureAwait(false);
            var leasesKeys = members.Select(x => (RedisKey)Keys.Lease(application, x)).ToArray();
            var leasesList =
                (await db.StringGetAsync(leasesKeys).ConfigureAwait(false)).Select(x =>
                    JsonConvert.DeserializeObject<Lease>(x)).ToList();
            return new ReadOnlyCollection<Lease>(leasesList);
        }

        public async Task<IReadOnlyCollection<Lease>> GetAssignedLeasesAsync(string application, string workerId,
            CancellationToken token)
        {
            var db = multiplexer.GetDatabase();
            var members = await db.SetMembersAsync(Keys.AssignedTo(application, workerId))
                .ConfigureAwait(false);
            var leasesKeys = members.Select(x => (RedisKey)Keys.Lease(application, x)).ToArray();
            var values = await db.StringGetAsync(leasesKeys).ConfigureAwait(false);
            var leases = values.Select(x => JsonConvert.DeserializeObject<Lease>(x)).ToList();
            return new ReadOnlyCollection<Lease>(leases);
        }
    }
}
