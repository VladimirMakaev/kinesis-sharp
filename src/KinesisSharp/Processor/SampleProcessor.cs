using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;
using Microsoft.Extensions.Logging;

namespace KinesisSharp.Processor
{
    public class SampleProcessor : IRecordsProcessor
    {
        private readonly ILogger<SampleProcessor> logger;

        public SampleProcessor(ILogger<SampleProcessor> logger)
        {
            this.logger = logger;
        }

        public Task ProcessRecordsAsync(IReadOnlyList<Record> records, RecordProcessingContext context)
        {
            logger.LogInformation("Retrieved {Count} records", records.Count);
            return Task.Delay(10);
            //return Task.CompletedTask;
        }
    }
}