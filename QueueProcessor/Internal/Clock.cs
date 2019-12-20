using System;

namespace QueueProcessor.Internal
{
    public sealed class Clock : IClock
    {
        public static readonly Clock Instance = new Clock();

        public DateTime Now => DateTime.UtcNow;
    }
}
