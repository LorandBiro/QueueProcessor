using QueueProcessor.Internal;
using System;

namespace QueueProcessor
{
    public sealed class RandomPollingStrategy : IPollingStrategy
    {
        private readonly TimeSpan minDelay;
        private readonly int repeatLimit;
        private readonly TimeSpan range;

        public RandomPollingStrategy(TimeSpan minDelay, TimeSpan maxDelay, int repeatLimit = int.MaxValue)
        {
            if (minDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "Minimum delay cannot be negative.");
            }

            if (maxDelay < minDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "Maximum delay cannot be smaller than the minimum delay.");
            }

            if (repeatLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatLimit), repeatLimit, "Repeat limit must be larger than zero.");
            }

            this.minDelay = minDelay;
            this.repeatLimit = repeatLimit;
            this.range = maxDelay - minDelay;
        }

        public TimeSpan GetDelay(int batchSize)
        {
            if (batchSize >= this.repeatLimit)
            {
                return TimeSpan.Zero;
            }

            return this.minDelay + this.range * ThreadLocalRandom.NextDouble();
        }
    }
}
