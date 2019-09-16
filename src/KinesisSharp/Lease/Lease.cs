namespace KinesisSharp.Lease
{
    public class Lease
    {
        public string ShardId { get; set; }

        public string Owner { get; set; }

        public ShardPosition Checkpoint { get; set; }

        public string RowVersion { get; set; }
    }
}