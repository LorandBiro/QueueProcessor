using QueueProcessor.Internal;
using QueueProcessor.Utils;
using System;

namespace QueueProcessor
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly double failureRateTreshold;
        private readonly IIntervalTimer intervalTimer;
        private readonly IFailureRateCalculator failureRateCalculator;
        private bool isOpen;

        public CircuitBreaker(double failureRateTreshold, TimeSpan interval, int bucketCount, TimeSpan bucketDuration)
            : this(failureRateTreshold, new IntervalTimer(interval, Clock.Instance), new FailureRateCalculator(bucketCount, bucketDuration))
        {
        }

        public CircuitBreaker(double failureRateTreshold, IIntervalTimer intervalTimer, IFailureRateCalculator failureRateCalculator)
        {
            if (failureRateTreshold <= 0.0 || failureRateTreshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(failureRateTreshold), failureRateTreshold, "Failure rate treshold must be greater than 0 and not greater than 1.");
            }

            this.failureRateTreshold = failureRateTreshold;
            this.intervalTimer = intervalTimer;
            this.failureRateCalculator = failureRateCalculator ?? throw new ArgumentNullException(nameof(failureRateCalculator));
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
                    this.isOpen = true;
                }
                else
                {
                    return null;
                }
            }

            return this.intervalTimer.GetDelay();
        }
    }
}
