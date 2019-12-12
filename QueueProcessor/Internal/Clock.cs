using System;

namespace QueueProcessor.Internal
{
    public sealed class Clock : IClock
    {
        public DateTime Now => DateTime.UtcNow;
    }
}
