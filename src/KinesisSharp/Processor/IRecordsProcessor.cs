using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Processor
{
    public interface IRecordsProcessor
    {
        Task ProcessRecordsAsync(IReadOnlyList<Record> records, RecordProcessingContext context);
    }
}