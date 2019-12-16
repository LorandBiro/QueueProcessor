using System;

namespace QueueProcessor
{
    public sealed class LongPollingStrategy : IPollingStrategy
    {
        public TimeSpan GetDelay(int batchSize) => TimeSpan.Zero;
    }
}
