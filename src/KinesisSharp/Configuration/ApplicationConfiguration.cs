using System;

namespace KinesisSharp.Configuration
{
    public class ApplicationConfiguration : StreamConfiguration
    {
        public int RecordsBatchLimit { get; set; } = 1000;

        public TimeSpan ShardPollDelay { get; set; } = TimeSpan.FromSeconds(2);

        public int NumberOfWorkers { get; set; } = 5;

        public string ApplicationName { get; set; }
    }
}