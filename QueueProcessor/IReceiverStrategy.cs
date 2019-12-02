using System;

namespace QueueProcessor
{
    public interface IReceiverStrategy
    {
        TimeSpan GetDelay(int batchSize);
    }

    public sealed class ContinuousReceiverStrategy : IReceiverStrategy
    {
        public TimeSpan GetDelay(int batchSize) => TimeSpan.Zero;
    }

    public sealed class UniformRandomReceiverStrategy : IReceiverStrategy
    {
        private static readonly Random Random = new Random();

        private readonly TimeSpan minDelay;
        private readonly int repeatLimit;
        private readonly TimeSpan range;

        public UniformRandomReceiverStrategy(TimeSpan minDelay, TimeSpan maxDelay, int repeatLimit = int.MaxValue)
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

            lock (Random)
            {
                return this.minDelay + this.range * Random.NextDouble();
            }
        }
    }

    public sealed class ConstantRateRandomReceiverStrategy : IReceiverStrategy
    {
        private static readonly Random Random = new Random();

        private readonly TimeSpan interval;
        private readonly int repeatLimit;
        private DateTime intervalStart;

        public ConstantRateRandomReceiverStrategy(TimeSpan interval, int repeatLimit = int.MaxValue)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be bigger than zero.");
            }

            if (repeatLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatLimit), repeatLimit, "Repeat limit must be larger than zero.");
            }

            this.interval = interval;
            this.repeatLimit = repeatLimit;
            this.intervalStart = DateTime.UtcNow;
        }

        public TimeSpan GetDelay(int batchSize)
        {
            lock (Random)
            {
                DateTime now = DateTime.UtcNow;

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
                    delay = minDelay + (maxDelay - minDelay) * Random.NextDouble();
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
}
