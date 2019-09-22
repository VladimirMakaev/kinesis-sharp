using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinesisSharp.Configuration;
using KinesisSharp.Shards;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp
{
    public class WorkerScheduler : IHostedService, IDisposable
    {
        private readonly ApplicationConfiguration applicationConfiguration;
        private readonly IDiscoverShards listShards;
        private readonly ILogger<WorkerScheduler> logger;
        private readonly CancellationTokenSource stoppingToken = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, Task> workerTasks = new ConcurrentDictionary<string, Task>();

        public WorkerScheduler(IOptions<ApplicationConfiguration> workerSchedulerConfiguration,
            ILogger<WorkerScheduler> logger, IDiscoverShards listShards)
        {
            this.logger = logger;
            this.listShards = listShards;
            applicationConfiguration = workerSchedulerConfiguration.Value;
        }

        public void Dispose()
        {
            stoppingToken.Cancel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var i in Enumerable.Range(1, applicationConfiguration.NumberOfWorkers))
            {
                var consumerId = Guid.NewGuid().ToString("N");
                workerTasks.AddOrUpdate(consumerId, CreateConsumerTask(consumerId, stoppingToken.Token),
                    (key, value) => value);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                stoppingToken.Cancel();
            }
            finally
            {
                await Task.WhenAny(
                    Task.WhenAll(workerTasks.Values),
                    Task.Delay(-1, cancellationToken)
                ).ConfigureAwait(false);
            }
        }

        private Task CreateConsumerTask(string id, CancellationToken cancellationToken)
        {
            return Task.Run(() => RunConsumer(id, cancellationToken), cancellationToken).ContinueWith(x =>
            {
                if (x.IsCanceled)
                {
                    OnTaskCancelled(id);
                }

                if (x.IsFaulted)
                {
                    OnTaskFaulted(id, x.Exception);
                }
            }, cancellationToken);
        }

        private void OnTaskCancelled(string consumer)
        {
            logger.LogInformation("Task for {id} has been cancelled", consumer);
        }

        private void OnTaskFaulted(string consumer, AggregateException e)
        {
            logger.LogError("Task for {id} has faulted. {Exception}", consumer, e.Flatten());
            workerTasks.TryUpdate(consumer, CreateConsumerTask(consumer, stoppingToken.Token), null);
        }


        public async Task RunConsumer(string consumerId, CancellationToken token)
        {
            logger.LogDebug("Starting consumer {Consumer}", consumerId);
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
    }
}