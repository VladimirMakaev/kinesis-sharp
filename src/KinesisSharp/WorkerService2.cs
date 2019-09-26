using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Common;
using KinesisSharp.Configuration;
using KinesisSharp.Leases;
using KinesisSharp.Leases.Registry;
using KinesisSharp.Processor;
using KinesisSharp.Records;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp
{
    /// <summary>
    ///     Base class for implementing a long running <see cref="IHostedService" />.
    /// </summary>
    public class WorkerService2 : IHostedService, IDisposable
    {
        private readonly ILeaseClaimingService claimService;
        private readonly IOptions<ApplicationConfiguration> configuration;
        private readonly IKinesisShardReaderFactory factory;
        private readonly ILogger<WorkerService2> logger;
        private readonly SemaphoreSlim maxWorkerConcurrencySemaphore;

        private readonly ConcurrentDictionary<string, Lease> myLeases = new ConcurrentDictionary<string, Lease>();
        private readonly IRecordsProcessor processor;
        private readonly ILeaseRegistryCommand registryCommand;
        private readonly ILeaseRegistryQuery registryQuery;
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private readonly WorkerIdentifier workerId;
        private Task leaseClaimTask;
        private Task leaseReaderTask;


        public WorkerService2(IOptions<ApplicationConfiguration> configuration, ILeaseClaimingService claimService,
            IKinesisShardReaderFactory factory, IRecordsProcessor processor, ILeaseRegistryQuery registryQuery,
            ILeaseRegistryCommand registryCommand, ILogger<WorkerService2> logger)
        {
            this.claimService = claimService;
            this.factory = factory;
            this.processor = processor;
            this.registryQuery = registryQuery;
            this.registryCommand = registryCommand;
            this.logger = logger;
            this.configuration = configuration;
            workerId = WorkerIdentifier.New();
            maxWorkerConcurrencySemaphore = new SemaphoreSlim(5, 5);
        }

        public virtual void Dispose()
        {
            stoppingCts.Cancel();
        }

        /// <summary>
        ///     Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            leaseClaimTask = ExecuteLeaseClaimTask(stoppingCts.Token);
            leaseReaderTask = ExecuteLeaseReaderTask(stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            var whenAny = Task.WhenAny(leaseClaimTask, leaseReaderTask);

            if (whenAny.IsCompleted)
            {
                return whenAny;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (leaseClaimTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(leaseClaimTask, Task.Delay(Timeout.Infinite, cancellationToken))
                    .ConfigureAwait(false);
            }
        }


        private async Task ExecuteLeaseClaimTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var myLeasesResponse = await claimService
                    .ClaimLeases(configuration.Value.ApplicationName, workerId.Id, token)
                    .ConfigureAwait(false);

                foreach (var myLease in myLeasesResponse)
                {
                    myLeases.AddOrUpdate(myLease.ShardId, myLease, (key, l) => myLease);
                }

                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task ExecuteLeaseReaderTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                logger.LogDebug("ExecuteLeaseReaderTask. Leases: {Leases}", myLeases.Select(x => x.Key));
                foreach (var eachLease in myLeases.Values)
                {
                    try
                    {
                        await maxWorkerConcurrencySemaphore.WaitAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    try
                    {
                        ProcessLease(token, eachLease).Ignore();
                    }
                    finally
                    {
                        maxWorkerConcurrencySemaphore.Release();
                    }
                }

                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task ProcessLease(CancellationToken token, Lease eachLease)
        {
            logger.LogDebug("Starting to process lease for {ShardId}", eachLease.ShardId);
            var shardRef = new ShardRef(eachLease.ShardId, null);

            var reader = await factory.CreateReaderAsync(shardRef, eachLease.Checkpoint, token)
                .ConfigureAwait(false);

            while (!reader.EndOfShard)
            {
                logger.LogDebug("Reading next batch from {ShardId}", shardRef.ShardId);
                await reader.ReadNextAsync(token).ConfigureAwait(false);

                await processor.ProcessRecordsAsync(reader.Records,
                        new RecordProcessingContext(shardRef, workerId.Id))
                    .ConfigureAwait(false);

                eachLease.Checkpoint =
                    new ShardPosition(reader.Records.Select(x => x.SequenceNumber).LastOrDefault() ?? "0");

                await registryCommand.UpdateLease(configuration.Value.ApplicationName, eachLease, token)
                    .ConfigureAwait(false);
            }

            eachLease.Checkpoint = ShardPosition.ShardEnd;
            await registryCommand.UpdateLease(configuration.Value.ApplicationName, eachLease, token)
                .ConfigureAwait(false);
        }
    }
}