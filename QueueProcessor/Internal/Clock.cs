using System;

namespace QueueProcessor.Internal
{
    public sealed class Clock : IClock
    {
        public DateTime GetCurrentInstant() => DateTime.UtcNow;
    }
}
