using System;

namespace KinesisSharp.Configuration
{
    public class ApplicationConfiguration : StreamConfiguration
    {
        public int RecordsBatchLimit { get; set; }

        public TimeSpan ShardPollDelay { get; set; }

        public int NumberOfWorkers { get; set; }

        public string ApplicationName { get; set; }
    }
}