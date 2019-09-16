namespace KinesisSharp.Processor
{
    public class RecordProcessingContext
    {
        public RecordProcessingContext(ShardRef shardRef, string workerId)
        {
            ShardRef = shardRef;
            WorkerId = workerId;
        }

        public ShardRef ShardRef { get; }

        public string WorkerId { get; }
    }
}