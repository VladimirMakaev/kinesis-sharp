using System;

namespace KinesisSharp.Common
{
    public class TimerProvider
    {
        private static ITimeProvider _innerProvider = new SystemTimeProvider();

        public static DateTime UtcNow => _innerProvider.UtcNow;

        public static void SetProvider(ITimeProvider provider)
        {
            _innerProvider = provider;
        }
    }

    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }


    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}