using QueueProcessor.Internal;
using QueueProcessor.Utils;
using System;

namespace QueueProcessor
{
    public sealed class IntervalPollingStrategy : IPollingStrategy
    {
        private readonly int repeatLimit;
        private readonly IIntervalTimer intervalTimer;

        public IntervalPollingStrategy(int repeatLimit, TimeSpan interval)
            : this(repeatLimit, new IntervalTimer(interval, Clock.Instance)) { }

        public IntervalPollingStrategy(int repeatLimit, IIntervalTimer intervalTimer)
        {
            if (repeatLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatLimit), repeatLimit, "Repeat limit must be larger than zero.");
            }

            this.repeatLimit = repeatLimit;
            this.intervalTimer = intervalTimer ?? throw new ArgumentNullException(nameof(intervalTimer));
        }

        public TimeSpan GetDelay(int batchSize) => batchSize >= this.repeatLimit ? TimeSpan.Zero : this.intervalTimer.GetDelay();
    }
}
