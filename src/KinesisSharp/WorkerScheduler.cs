using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinesisSharp
{
    public class WorkerSchedulerConfiguration
    {
        public int NumberOfWorkers { get; set; }
    }

    public class WorkerScheduler : IHostedService, IDisposable
    {
        private readonly ILogger<WorkerScheduler> logger;
        private readonly CancellationTokenSource stoppingToken = new CancellationTokenSource();
        private readonly WorkerSchedulerConfiguration workerSchedulerConfiguration;
        private readonly ConcurrentDictionary<string, Task> workerTasks = new ConcurrentDictionary<string, Task>();

        public WorkerScheduler(IOptions<WorkerSchedulerConfiguration> workerSchedulerConfiguration,
            ILogger<WorkerScheduler> logger)
        {
            this.logger = logger;
            this.workerSchedulerConfiguration = workerSchedulerConfiguration.Value;
        }

        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var i in Enumerable.Range(1, workerSchedulerConfiguration.NumberOfWorkers))
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
                );
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
            logger.LogWarning("Task for {id} has faulted", consumer);
            workerTasks.TryRemove(consumer, out var task);
            CreateConsumerTask(consumer, stoppingToken.Token);
        }


        public async Task RunConsumer(string consumerId, CancellationToken token)
        {
            var i = 0;
            while (!token.IsCancellationRequested)
            {
                logger.LogInformation("Tick {Id}", consumerId);
                await Task.Delay(1000, token);
                i++;

                if (i == 10)
                {
                    throw new InvalidOperationException("Fatal Crash");
                }
            }
        }
    }
}