using QueueProcessor.Internal;
using System;
using System.Threading;

namespace QueueProcessor
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly double failureRateTreshold;
        private readonly long interval;
        private readonly IFailureRateCalculator failureRateCalculator;
        private readonly IClock clock;
        private bool isOpen;
        private long intervalStart;

        public CircuitBreaker(double failureRateTreshold, TimeSpan interval, int bucketCount, TimeSpan bucketDuration)
            : this(failureRateTreshold, interval, new FailureRateCalculator(bucketCount, bucketDuration), new Clock())
        {
        }

        public CircuitBreaker(double failureRateTreshold, TimeSpan interval, IFailureRateCalculator failureRateCalculator, IClock clock)
        {
            if (failureRateTreshold <= 0.0 || failureRateTreshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(failureRateTreshold), failureRateTreshold, "Failure rate treshold must be greater than 0 and not greater than 1.");
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be bigger than zero.");
            }

            this.failureRateTreshold = failureRateTreshold;
            this.interval = interval.Ticks;
            this.failureRateCalculator = failureRateCalculator ?? throw new ArgumentNullException(nameof(failureRateCalculator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void OnSuccess() => this.failureRateCalculator.OnSuccess();
        public void OnFailure() => this.failureRateCalculator.OnFailure();

        public TimeSpan? GetDelay()
        {
            double failureRate = this.failureRateCalculator.GetFailureRate();
            if (this.isOpen)
            {
                if (failureRate < this.failureRateTreshold)
                {
                    this.isOpen = false;
                    return null;
                }
            }
            else
            {
                if (failureRate > this.failureRateTreshold)
                {
                    this.intervalStart = this.clock.Now.Ticks;
                    this.isOpen = true;
                }
                else
                {
                    return null;
                }
            }

            long now = this.clock.Now.Ticks;
            long start = this.intervalStart;
            long end = start + this.interval;
            long minDelay = start > now ? start - now : 0;
            long maxDelay = end > now ? end - now : 0;
            long delay = minDelay + (long)((maxDelay - minDelay) * ThreadLocalRandom.NextDouble());

            long scheduledPoll = now + delay;
            while (start < scheduledPoll)
            {
                end = start + this.interval;
                long originalStart = Interlocked.CompareExchange(ref this.intervalStart, end, start);
                start = originalStart == start ? end : originalStart;
            }

            return new TimeSpan(delay);
        }
    }
}
