using QueueProcessor.Internal;
using System;

namespace QueueProcessor
{
    public sealed class FixedIntervalReceiverStrategy : IReceiverStrategy
    {
        private readonly IClock clock;
        private readonly TimeSpan interval;
        private readonly int repeatLimit;
        private DateTime intervalStart;

        public FixedIntervalReceiverStrategy(IClock clock, TimeSpan interval, int repeatLimit = int.MaxValue)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be bigger than zero.");
            }

            if (repeatLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatLimit), repeatLimit, "Repeat limit must be larger than zero.");
            }

            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.interval = interval;
            this.repeatLimit = repeatLimit;
            this.intervalStart = this.clock.Now;
        }

        public TimeSpan GetDelay(int batchSize)
        {
            DateTime now = this.clock.Now;

            TimeSpan delay;
            if (batchSize >= this.repeatLimit)
            {
                delay = TimeSpan.Zero;
            }
            else
            {
                DateTime intervalEnd = this.intervalStart + this.interval;
                TimeSpan minDelay = this.intervalStart > now ? this.intervalStart - now : TimeSpan.Zero;
                TimeSpan maxDelay = intervalEnd > now ? intervalEnd - now : TimeSpan.Zero;
                delay = minDelay + (maxDelay - minDelay) * ThreadLocalRandom.NextDouble();
            }

            DateTime scheduledPoll = now + delay;
            while (this.intervalStart < scheduledPoll)
            {
                this.intervalStart += this.interval;
            }

            return delay;
        }
    }
}
