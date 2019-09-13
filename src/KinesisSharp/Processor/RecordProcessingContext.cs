namespace KinesisSharp.Processor
{
    public class RecordProcessingContext
    {
        public RecordProcessingContext(ShardRef shardRef, string consumerId)
        {
            ShardRef = shardRef;
            ConsumerId = consumerId;
        }

        public ShardRef ShardRef { get; }

        public string ConsumerId { get; }
    }
}