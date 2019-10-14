using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using KinesisSharp.Configuration;
using Microsoft.Extensions.Options;

namespace KinesisSharp.Records
{
    public interface IKinesisShardReaderFactory
    {
        Task<IKinesisShardReader> CreateReaderAsync(string shardId, ShardPosition position,
            CancellationToken token = default);
    }

    public class KinesisShardReaderFactory : IKinesisShardReaderFactory
    {
        private readonly IOptions<ApplicationConfiguration> configuration;
        private readonly IAmazonKinesis kinesisClient;

        public KinesisShardReaderFactory(IOptions<ApplicationConfiguration> configuration, IAmazonKinesis kinesisClient)
        {
            this.configuration = configuration;
            this.kinesisClient = kinesisClient;
        }

        public async Task<IKinesisShardReader> CreateReaderAsync(string shardId, ShardPosition position,
            CancellationToken token = default)
        {
            var shardIterator = await kinesisClient.GetShardIteratorAsync(
                new GetShardIteratorRequest
                {
                    ShardId = shardId,
                    StreamName = configuration.Value.StreamArn,
                    ShardIteratorType = ToIteratorType(position),
                    //Timestamp = configuration.Value.TimeStamp.GetValueOrDefault(),
                    StartingSequenceNumber = ToStartingSequenceNumber(position)
                }, token).ConfigureAwait(false);

            return new KinesisReader(shardIterator.ShardIterator, kinesisClient, configuration.Value.RecordsBatchLimit);
        }


        private string ToStartingSequenceNumber(ShardPosition position)
        {
            if (Equals(position, ShardPosition.TrimHorizon))
            {
                return null;
            }

            return position.SequenceNumber;
        }

        private ShardIteratorType ToIteratorType(ShardPosition position)
        {
            if (Equals(position, ShardPosition.TrimHorizon))
            {
                return ShardIteratorType.TRIM_HORIZON;
            }


            return ShardIteratorType.AT_SEQUENCE_NUMBER;
        }

        public class KinesisReader : IKinesisShardReader
        {
            private readonly int batchLimit;
            private readonly IAmazonKinesis kinesisClient;

            public KinesisReader(string shardIterator, IAmazonKinesis kinesisClient, int batchLimit)
            {
                ShardIterator = shardIterator;
                this.kinesisClient = kinesisClient;
                this.batchLimit = batchLimit;
                EndOfShard = shardIterator == null;
            }

            public string ShardIterator { get; private set; }
            public bool EndOfShard { get; private set; }
            public long? MillisBehindLatest { get; private set; }
            public IReadOnlyList<Record> Records { get; private set; }

            public async Task ReadNextAsync(CancellationToken token = default)
            {
                var response = await kinesisClient
                    .GetRecordsAsync(new GetRecordsRequest {ShardIterator = ShardIterator, Limit = batchLimit}, token)
                    .ConfigureAwait(false);

                ShardIterator = response.NextShardIterator;
                Records = new ReadOnlyCollection<Record>(response.Records);
                MillisBehindLatest = response.MillisBehindLatest;
                EndOfShard = response.NextShardIterator == null;
            }
        }
    }
}
