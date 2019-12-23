using System;

namespace QueueProcessor.Utils
{
    public sealed class Clock : IClock
    {
        public static readonly Clock Instance = new Clock();

        public DateTime Now => DateTime.UtcNow;
    }
}
