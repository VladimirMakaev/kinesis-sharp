using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;

namespace KinesisSharp.Records
{
    public interface IKinesisShardReader
    {
        bool EndOfShard { get; }

        long? MillisBehindLatest { get; }
        IReadOnlyList<Record> Records { get; }
        Task ReadNextAsync(CancellationToken token = default);
    }
}