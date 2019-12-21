using QueueProcessor.Internal;
using System;
using System.Threading;

namespace QueueProcessor.Utils
{
    public class IntervalTimer : IIntervalTimer
    {
        private readonly long interval;
        private readonly IClock clock;
        private long intervalStart;

        public IntervalTimer(TimeSpan interval, IClock clock)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be bigger than zero.");
            }

            this.interval = interval.Ticks;
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.intervalStart = this.clock.Now.Ticks;
        }

        public TimeSpan GetDelay()
        {
            long now = this.clock.Now.Ticks;
            long start = this.intervalStart;
            long end = start + this.interval;

            long add = this.interval;
            long delay;
            if (now > end)
            {
                delay = 0;
                add = (now - start) / this.interval * this.interval;
            }
            else if (now > start)
            {
                long maxDelay = end - now;
                delay = (long)(maxDelay * ThreadLocalRandom.NextDouble());
            }
            else
            {
                long minDelay = start - now;
                long maxDelay = end - now;
                delay = minDelay + (long)((maxDelay - minDelay) * ThreadLocalRandom.NextDouble());
            }

            Interlocked.Add(ref this.intervalStart, add);
            return new TimeSpan(delay);
        }
    }
}
