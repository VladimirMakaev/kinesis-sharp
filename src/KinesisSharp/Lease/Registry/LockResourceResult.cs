namespace KinesisSharp.Lease.Registry
{
    public class LockResourceResult
    {
        public string LockId { get; set; }

        public bool Success { get; set; }
    }
}