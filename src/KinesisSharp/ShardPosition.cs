namespace KinesisSharp
{
    public class ShardPosition
    {
        public static ShardPosition TrimHorizon = new ShardPosition("TRIM_HORIZON");

        public static ShardPosition ShardEnd = new ShardPosition("SHARD_END");

        public ShardPosition(string sequenceNumber)
        {
            SequenceNumber = sequenceNumber;
        }

        public string SequenceNumber { get; }

        public bool IsEnded => Equals(this, ShardEnd);

        public bool IsStarted => !Equals(this, TrimHorizon);

        protected bool Equals(ShardPosition other)
        {
            return string.Equals(SequenceNumber, other.SequenceNumber);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShardPosition) obj);
        }

        public override int GetHashCode()
        {
            return SequenceNumber != null ? SequenceNumber.GetHashCode() : 0;
        }
    }
}