namespace KinesisSharp.Lease.Registry
{
    public class UpdateLeaseResult
    {
        public bool Success { get; set; }

        public Lease Lease { get; set; }
    }
}