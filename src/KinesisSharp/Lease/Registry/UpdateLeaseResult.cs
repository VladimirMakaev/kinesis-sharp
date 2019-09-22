namespace KinesisSharp.Lease.Registry
{
    public class UpdateLeaseResult
    {
        private UpdateLeaseResult(bool success, Lease lease, LeaseRegistryOperationError? fail = null)
        {
            IsSuccess = success;
            Lease = lease;
            Error = fail;
        }

        public bool IsSuccess { get; }

        public Lease Lease { get; }
        public LeaseRegistryOperationError? Error { get; }

        public static UpdateLeaseResult Success(Lease lease)
        {
            return new UpdateLeaseResult(true, lease);
        }

        public static UpdateLeaseResult Fail(LeaseRegistryOperationError error)
        {
            return new UpdateLeaseResult(false, null, error);
        }
    }

    public enum LeaseRegistryOperationError
    {
        StaleData,
        NotFound
    }
}