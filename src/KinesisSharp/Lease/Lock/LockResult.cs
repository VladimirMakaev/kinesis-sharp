namespace KinesisSharp.Lease.Lock
{
    public class LockResult
    {
        private LockResult(bool isSuccess, Lock @lock, LockError? error)
        {
            IsSuccess = isSuccess;
            Lock = @lock;
            Error = error;
        }

        public bool IsSuccess { get; }

        public Lock Lock { get; }
        public LockError? Error { get; }

        public static LockResult Success(Lock @lock)
        {
            return new LockResult(true, @lock, null);
        }

        public static LockResult Fail(LockError error)
        {
            return new LockResult(false, null, error);
        }
    }

    public enum LockError
    {
        AlreadyLocked,
        LockNotFound
    }
}