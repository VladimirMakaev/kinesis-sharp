namespace KinesisSharp.Lease
{
    public class TakeLeaseResult
    {
        public TakeLeaseResult(bool success, Lease lease)
        {
            Success = success;
            Lease = lease;
        }

        public bool Success { get; }

        public Lease Lease { get; }
    }
}