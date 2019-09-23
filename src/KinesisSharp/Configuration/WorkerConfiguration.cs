using System;

namespace KinesisSharp.Configuration
{
    public class WorkerConfiguration
    {
        public int MaxLeasesPerClaim { get; set; } = 2;

        public int MaxLeasesPerWorker { get; set; } = 5;

        public TimeSpan LeaseLockDuration { get; set; } = TimeSpan.FromSeconds(30);
    }
}