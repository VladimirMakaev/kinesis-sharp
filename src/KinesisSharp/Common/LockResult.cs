using KinesisSharp.Leases.Lock;

namespace KinesisSharp.Common
{
    public class LockResult : Result<Lock>
    {
        private LockResult(bool isSuccess, Lock @lock, Errors? error) : base(isSuccess, @lock, error)
        {
        }

        public static LockResult Success(Lock @lock)
        {
            return new LockResult(true, @lock, null);
        }

        public static LockResult Fail(Errors error)
        {
            return new LockResult(false, null, error);
        }
    }
}
