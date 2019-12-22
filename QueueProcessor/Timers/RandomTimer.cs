using QueueProcessor.Internal;
using System;

namespace QueueProcessor.Timers
{
    public sealed class RandomTimer : ITimer
    {
        private readonly TimeSpan minDelay;
        private readonly TimeSpan range;

        public RandomTimer(TimeSpan maxDelay)
            : this(TimeSpan.Zero, maxDelay) { }

        public RandomTimer(TimeSpan minDelay, TimeSpan maxDelay)
        {
            if (minDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "Minimum delay cannot be negative.");
            }

            if (maxDelay < minDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "Maximum delay cannot be smaller than the minimum delay.");
            }

            this.minDelay = minDelay;
            this.range = maxDelay - minDelay;
        }

        public TimeSpan GetDelay() => this.minDelay + this.range * ThreadLocalRandom.NextDouble();
    }
}
