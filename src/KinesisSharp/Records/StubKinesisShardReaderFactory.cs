using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using KinesisSharp.Common;

namespace KinesisSharp.Records
{
    public class StubKinesisShardReaderFactory : IKinesisShardReaderFactory
    {
        private static long globalSequence;
        private readonly int batchLimit;
        private readonly int messagesPerShard;

        public StubKinesisShardReaderFactory(int messagesPerShard, int batchLimit)
        {
            this.messagesPerShard = messagesPerShard;
            this.batchLimit = batchLimit;
        }

        public async Task<IKinesisShardReader> CreateReaderAsync(string shardId, ShardPosition position,
            CancellationToken token = default)
        {
            await Task.CompletedTask;
            return new StubKinesisShardReader(position, messagesPerShard,
                messagesPerShard * 100, batchLimit,
                shardId);
        }

        private class StubKinesisShardReader : IKinesisShardReader
        {
            private readonly int batchLimit;
            private readonly int lengthInMillis;
            private readonly int numberOfRecords;
            private readonly string shardId;
            private int currentPointer;

            public StubKinesisShardReader(ShardPosition position, int numberOfRecords, int lengthInMillis,
                int batchLimit, string shardId)
            {
                this.numberOfRecords = numberOfRecords;
                this.batchLimit = batchLimit;
                this.shardId = shardId;
                EndOfShard = position.IsEnded;
                MillisBehindLatest = lengthInMillis;
                this.lengthInMillis = lengthInMillis;
                Records = new ReadOnlyCollection<Record>(new List<Record>());
                currentPointer = 0;
            }


            public bool EndOfShard { get; private set; }
            public long? MillisBehindLatest { get; private set; }
            public IReadOnlyList<Record> Records { get; private set; }

            public Task ReadNextAsync(CancellationToken token = default)
            {
                var messages = Enumerable.Range(currentPointer, batchLimit).Select(x => new Record
                {
                    ApproximateArrivalTimestamp =
                        TimerProvider.UtcNow - TimeSpan.FromMilliseconds(MillisBehindLatest ?? 0),
                    Data = new MemoryStream(Encoding.UTF8.GetBytes($"Shard-{shardId}-Message-{x}")),
                    EncryptionType = EncryptionType.NONE,
                    PartitionKey = "SomeKey",
                    SequenceNumber = $"{Interlocked.Increment(ref globalSequence)}"
                }).ToList();

                currentPointer += messages.Count;
                MillisBehindLatest = (numberOfRecords - currentPointer) * lengthInMillis / numberOfRecords;
                EndOfShard = currentPointer >= numberOfRecords;
                Records = new ReadOnlyCollection<Record>(messages);
                return Task.CompletedTask;
            }
        }
    }
}