using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Kinesis.Model.Internal.MarshallTransformations;
using KinesisSharp.Common;

namespace KinesisSharp.Records
{
    public class StubKinesisShardReaderFactory : IKinesisShardReaderFactory
    {
        private readonly int messagesPerShard;
        private readonly int batchLimit;

        private static long globalSequence = 0;

        public StubKinesisShardReaderFactory(int messagesPerShard, int batchLimit)
        {
            this.messagesPerShard = messagesPerShard;
            this.batchLimit = batchLimit;
        }

        public Task<IKinesisShardReader> CreateReaderAsync(ShardRef shardRef, ShardPosition position,
            CancellationToken token = default)
        {
            return Task.FromResult((IKinesisShardReader)new StubKinesisShardReader(messagesPerShard, messagesPerShard * 100, batchLimit,
                shardRef.ShardId));
        }

        private class StubKinesisShardReader : IKinesisShardReader
        {
            private readonly int numberOfRecords;
            private readonly int batchLimit;
            private readonly string shardId;
            private int currentPointer;
            private int lengthInMillis;

            public StubKinesisShardReader(int numberOfRecords, int lengthInMillis, int batchLimit, string shardId)
            {
                this.numberOfRecords = numberOfRecords;
                this.batchLimit = batchLimit;
                this.shardId = shardId;
                EndOfShard = false;
                MillisBehindLatest = lengthInMillis;
                this.lengthInMillis = lengthInMillis;
                Records = new ReadOnlyCollection<Record>(new List<Record>());
                currentPointer = 0;
            }


            public bool EndOfShard { get; }
            public long? MillisBehindLatest { get; private set; }
            public IReadOnlyList<Record> Records { get; }

            public Task ReadNextAsync(CancellationToken token = default)
            {
                var messages = Enumerable.Range(currentPointer, batchLimit).Select(x => new Record()
                {
                    ApproximateArrivalTimestamp =
                        TimerProvider.UtcNow - TimeSpan.FromMilliseconds(this.MillisBehindLatest ?? 0),
                    Data = new MemoryStream(Encoding.UTF8.GetBytes($"Shard-{shardId}-Message-{x}")),
                    EncryptionType = EncryptionType.NONE,
                    PartitionKey = "SomeKey",
                    SequenceNumber = $"{Interlocked.Increment(ref globalSequence)}"
                }).ToList();

                this.currentPointer += messages.Count;
                this.MillisBehindLatest = ((numberOfRecords - currentPointer) * lengthInMillis) / this.numberOfRecords;
                return Task.CompletedTask;
            }
        }
    }
}