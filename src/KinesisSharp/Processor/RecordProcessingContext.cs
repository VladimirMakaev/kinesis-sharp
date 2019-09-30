namespace KinesisSharp.Processor
{
    public class RecordProcessingContext
    {
        public RecordProcessingContext(string shardId, string workerId)
        {
            ShardId = shardId;
            WorkerId = workerId;
        }

        public string ShardId { get; }
        public string WorkerId { get; }
    }
}