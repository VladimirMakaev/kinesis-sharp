using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Processor
{
    public class SampleProcessor : IRecordsProcessorAsync
    {
        public Task ProcessRecordsAsync(IReadOnlyList<Record> records, RecordProcessingContext context)
        {
            Console.WriteLine("Retrieved {0} records", records.Count);
            return Task.CompletedTask;
        }
    }
}