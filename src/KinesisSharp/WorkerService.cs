﻿using System;
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
    public class WorkerService : IHostedService, IDisposable
    {
        private readonly ILeaseClaimingService claimService;
        private readonly IOptions<ApplicationConfiguration> configuration;
        private readonly IKinesisShardReaderFactory factory;
        private readonly ILogger<WorkerService> logger;
        private readonly SemaphoreSlim maxWorkerConcurrencySemaphore;

        private readonly IRecordsProcessor processor;
        private readonly ILeaseRegistryCommand registryCommand;
        private readonly ILeaseRegistryQuery registryQuery;
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private readonly WorkerIdentifier workerId;
        private Task leaseClaimTask;
        private Task leaseReaderTask;


        public WorkerService(IOptions<ApplicationConfiguration> configuration, ILeaseClaimingService claimService,
            IKinesisShardReaderFactory factory, IRecordsProcessor processor, ILeaseRegistryQuery registryQuery,
            ILeaseRegistryCommand registryCommand, ILogger<WorkerService> logger)
        {
            this.claimService = claimService;
            this.factory = factory;
            this.processor = processor;
            this.registryQuery = registryQuery;
            this.registryCommand = registryCommand;
            this.logger = logger;
            this.configuration = configuration;
            workerId = WorkerIdentifier.New();
            maxWorkerConcurrencySemaphore = new SemaphoreSlim(configuration.Value.NumberOfWorkers);
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
                await claimService
                    .ClaimLeases(configuration.Value.ApplicationName, workerId.Id, token)
                    .ConfigureAwait(false);

                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task ExecuteLeaseReaderTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var eachLease in await registryQuery
                    .GetAssignedLeasesAsync(configuration.Value.ApplicationName, workerId.Id, token)
                    .ConfigureAwait(false))
                {
                    try
                    {
                        await maxWorkerConcurrencySemaphore.WaitAsync(token).ConfigureAwait(false);
                        logger.LogDebug("Entered Semaphore. Count: {Count}",
                            maxWorkerConcurrencySemaphore.CurrentCount);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    ProcessLease(token, eachLease).Ignore();
                }

                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task ProcessLease(CancellationToken token, Lease eachLease)
        {
            try
            {
                logger.LogDebug("Starting to process lease for {ShardId}", eachLease.ShardId);

                var reader = await factory.CreateReaderAsync(eachLease.ShardId, eachLease.Checkpoint, token)
                    .ConfigureAwait(false);

                while (!reader.EndOfShard || reader.MillisBehindLatest == 0)
                {
                    logger.LogDebug("Reading next batch from {ShardId}", eachLease.ShardId);

                    await reader.ReadNextAsync(token).ConfigureAwait(false);

                    await processor.ProcessRecordsAsync(reader.Records,
                            new RecordProcessingContext(eachLease.ShardId, workerId.Id))
                        .ConfigureAwait(false);


                    if (reader.Records.Count > 0)
                    {
                        eachLease.Checkpoint =
                            new ShardPosition(reader.Records.Select(x => x.SequenceNumber).Last());
                    }

                    await registryCommand.UpdateLease(configuration.Value.ApplicationName, eachLease, token)
                        .ConfigureAwait(false);

                    await Task.Delay(configuration.Value.ShardPollDelay, token).ConfigureAwait(false);
                }

                if (reader.EndOfShard)
                {
                    eachLease.Checkpoint = ShardPosition.ShardEnd;
                    logger.LogWarning("{Shard} has finished", eachLease.ShardId);
                }
                else
                {
                    logger.LogInformation($"{reader.MillisBehindLatest}ms behind latest for shard {eachLease.ShardId}");
                }


                await registryCommand.UpdateLease(configuration.Value.ApplicationName, eachLease, token)
                    .ConfigureAwait(false);
            }
            finally
            {
                maxWorkerConcurrencySemaphore.Release();
            }
        }
    }
}
