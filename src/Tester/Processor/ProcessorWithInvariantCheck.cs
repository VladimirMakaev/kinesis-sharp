using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinesisSharp.Processor;
using StackExchange.Redis;

namespace Tester.Processor
{
    public class ProcessorWithInvariantCheck : IRecordsProcessor
    {
        private readonly IConnectionMultiplexer multiplexer;

        public ProcessorWithInvariantCheck(IConnectionMultiplexer multiplexer)
        {
            this.multiplexer = multiplexer;
        }

        public async Task ProcessRecordsAsync(IReadOnlyList<Amazon.Kinesis.Model.Record> records,
            RecordProcessingContext context)
        {
            var db = multiplexer.GetDatabase(0);
            db.StringSet($"W:All:{context.ShardId}", DateTime.UtcNow.ToString());

            foreach (var record in records)
            {
                var key = $"W:{context.WorkerId}:{context.ShardId}";
                await db.StringSetAsync(key, 0, null, When.NotExists);
                await db.StringIncrementAsync(key);
            }
        }
    }
}
