using QueueProcessor.Internal;
using System;
using System.Threading;

namespace QueueProcessor
{
    public sealed class IntervalPollingStrategy : IPollingStrategy
    {
        private readonly IClock clock;
        private readonly long interval;
        private readonly int repeatLimit;
        private long intervalStart;

        public IntervalPollingStrategy(TimeSpan interval)
            : this(interval, int.MaxValue) { }

        public IntervalPollingStrategy(TimeSpan interval, int repeatLimit)
            : this(interval, repeatLimit, Clock.Instance) { }

        public IntervalPollingStrategy(TimeSpan interval, int repeatLimit, IClock clock)
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
            this.interval = interval.Ticks;
            this.repeatLimit = repeatLimit;
            this.intervalStart = this.clock.Now.Ticks;
        }

        public TimeSpan GetDelay(int batchSize)
        {
            long now = this.clock.Now.Ticks;
            long start = this.intervalStart;

            long delay;
            if (batchSize >= this.repeatLimit)
            {
                delay = 0;
            }
            else
            {
                long end = start + this.interval;
                long minDelay = start > now ? start - now : 0;
                long maxDelay = end > now ? end - now : 0;
                delay = minDelay + (long)((maxDelay - minDelay) * ThreadLocalRandom.NextDouble());
            }

            long scheduledPoll = now + delay;
            while (start < scheduledPoll)
            {
                long end = start + this.interval;
                long originalStart = Interlocked.CompareExchange(ref this.intervalStart, end, start);
                start = originalStart == start ? end : originalStart;
            }

            return new TimeSpan(delay);
        }
    }
}
