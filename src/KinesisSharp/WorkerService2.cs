using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace KinesisSharp
{
    /// <summary>
    ///     Base class for implementing a long running <see cref="IHostedService" />.
    /// </summary>
    public class WorkerService2 : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
         private Task leaseClaimTask;
        private Task readerTask;

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
            readerTask = ExecuteReaderTask(stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            var whenAny = Task.WhenAny(leaseClaimTask, readerTask);
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
            while (true)
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }

        private async Task ExecuteReaderTask(CancellationToken token)
        {
            while (true)
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
        }
    }
}