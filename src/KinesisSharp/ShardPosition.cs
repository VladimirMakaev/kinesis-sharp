namespace KinesisSharp
{
    public class ShardPosition
    {
        public static ShardPosition TrimHorizon = new ShardPosition("TRIM_HORIZON");

        public static ShardPosition ShardEnd = new ShardPosition("SHARD_END");

        private readonly string sequenceNumber;

        public ShardPosition(string sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }

        public string SequenceNumber => sequenceNumber;
    }
}