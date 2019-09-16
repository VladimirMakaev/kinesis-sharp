using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Configuration;
using KinesisSharp.Lease.Registry;
using KinesisSharp.Processor;
using KinesisSharp.Records;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp
{
    public class WorkerService : BackgroundService
    {
        private readonly IOptions<ApplicationConfiguration> configuration;
        private readonly ILeaseRegistryCommand leaseCommand;
        private readonly ILeaseRegistryQuery leaseQuery;
        private readonly ILogger<WorkerService> logger;
        private readonly IKinesisShardReaderFactory readerFactory;

        private readonly IDictionary<string, IKinesisShardReader> readers =
            new Dictionary<string, IKinesisShardReader>();

        private readonly IRecordsProcessor recordsProcessor;
        private readonly WorkerIdentifier workerId;

        public WorkerService(IOptions<ApplicationConfiguration> configuration,
            ILeaseRegistryQuery leaseQuery, ILeaseRegistryCommand leaseCommand, IRecordsProcessor recordsProcessor,
            ILogger<WorkerService> logger,
            IKinesisShardReaderFactory readerFactory)
        {
            this.configuration = configuration;
            workerId = WorkerIdentifier.New();
            this.leaseQuery = leaseQuery;
            this.leaseCommand = leaseCommand;
            this.recordsProcessor = recordsProcessor;
            this.logger = logger;
            this.readerFactory = readerFactory;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting Worker {WorkerId}", workerId.Id);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping Worker {WorkerId}", workerId.Id);
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                var config = configuration.Value;

                var leases = await leaseQuery.GetAssignedLeasesAsync(config.ApplicationName, workerId.Id, stoppingToken)
                    .ConfigureAwait(false);

                foreach (var lease in leases)
                {
                    try
                    {
                        var shardReader = await GetOrCreateReaderForLease(lease, stoppingToken).ConfigureAwait(false);
                        if (!shardReader.EndOfShard)
                        {
                            await shardReader.ReadNextAsync(stoppingToken).ConfigureAwait(false);

                            await recordsProcessor.ProcessRecordsAsync(shardReader.Records,
                                new RecordProcessingContext(null, workerId.Id)).ConfigureAwait(false);
                        }


                        if (shardReader.EndOfShard)
                        {
                            //TODO: Checkpoint at the end of shard
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, e.Message);
                    }

                    await Task.Delay(configuration.Value.ShardPollDelay, stoppingToken).ConfigureAwait(false);
                }
            }
        }


        public async Task<IKinesisShardReader> GetOrCreateReaderForLease(Lease.Lease lease, CancellationToken token)
        {
            if (readers.ContainsKey(lease.ShardId))
            {
                return readers[lease.ShardId];
            }

            var result = await readerFactory
                .CreateReaderAsync(new ShardRef(lease.ShardId, null), lease.Checkpoint, token)
                .ConfigureAwait(false);
            readers[lease.ShardId] = result;
            return result;
        }
    }

    public class WorkerIdentifier
    {
        private WorkerIdentifier(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static WorkerIdentifier New()
        {
            return new WorkerIdentifier("workerId-" + Guid.NewGuid().ToString("N"));
        }
    }
}