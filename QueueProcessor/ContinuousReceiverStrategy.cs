using System;

namespace QueueProcessor
{
    public sealed class ContinuousReceiverStrategy : IReceiverStrategy
    {
        public TimeSpan GetDelay(int batchSize) => TimeSpan.Zero;
    }
}
