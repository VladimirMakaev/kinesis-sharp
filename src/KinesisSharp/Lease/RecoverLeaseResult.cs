namespace KinesisSharp.Lease
{
    public class RecoverLeaseResult
    {
        public RecoverLeaseResult(bool success, Lease lease)
        {
            Success = success;
            Lease = lease;
        }

        public bool Success { get; }

        public Lease Lease { get; }
    }
}