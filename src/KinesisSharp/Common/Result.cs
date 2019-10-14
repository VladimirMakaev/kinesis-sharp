namespace KinesisSharp.Common
{
    public class Result
    {
        public static Result<T> Success<T>(T @object)
        {
            return new Result<T>(true, @object, null);
        }

        public static Result<T> Fail<T>(Errors error)
        {
            return new Result<T>(false, default, error);
        }
    }

    public class Result<T>
    {
        internal Result(bool isSuccess, T @lock, Errors? error)
        {
            IsSuccess = isSuccess;
            Lock = @lock;
            Error = error;
        }

        public bool IsSuccess { get; }

        public T Lock { get; }
        public Errors? Error { get; }
    }
}
