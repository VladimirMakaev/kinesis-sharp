using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Configuration;
using KinesisSharp.Leases.Discovery;
using KinesisSharp.Leases.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp
{
    public class LeaseDiscoveryWorker : BackgroundService
    {
        private readonly IOptions<ApplicationConfiguration> configuration;
        private readonly ILeaseDiscoveryService leaseDiscoveryService;
        private readonly ILogger<LeaseDiscoveryWorker> logger;
        private readonly ILeaseRegistryCommand registryCommand;
        private readonly ILeaseRegistryQuery registryQuery;

        public LeaseDiscoveryWorker(ILogger<LeaseDiscoveryWorker> logger,
            IOptions<ApplicationConfiguration> configuration, ILeaseDiscoveryService leaseDiscoveryService,
            ILeaseRegistryCommand registryCommand, ILeaseRegistryQuery registryQuery)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.leaseDiscoveryService = leaseDiscoveryService;
            this.registryCommand = registryCommand;
            this.registryQuery = registryQuery;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            {
                var response = await leaseDiscoveryService.ResolveLeasesForShards(stoppingToken)
                    .ConfigureAwait(false);

                logger.LogDebug("LeasesToBeCreated: {NewShards}. LeasesToBeDeleted: {ToBeDeleted}",
                    string.Join(",", response.LeasesToBeCreated.Select(x => x.ShardId)),
                    string.Join(",", response.LeasesToBeDeleted.Select(x => x.ShardId))
                );

                foreach (var lease in response.LeasesToBeCreated)
                {
                    await registryCommand.CreateLease(configuration.Value.ApplicationName, lease,
                        stoppingToken).ConfigureAwait(false);
                }

                foreach (var lease in response.LeasesToBeDeleted)
                {
                    await registryCommand.DeleteLease(configuration.Value.ApplicationName, lease.ShardId, stoppingToken)
                        .ConfigureAwait(false);
                }

                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}