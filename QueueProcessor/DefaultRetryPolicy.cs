using System;

namespace QueueProcessor
{
    public class DefaultRetryPolicy : IRetryPolicy
    {
        private static readonly Random Random = new Random();

        private readonly int maxRetryExponent;

        public DefaultRetryPolicy(int maxRetryExponent)
        {
            this.maxRetryExponent = maxRetryExponent;
        }

        // Instances of this class will be used from concurrent threads but it looks like we don't have to use locks for increasing and decreasing this
        // property. Race conditions can occur so updates can be lost, it's value can go above the maximum, but it cannot go below zero. So there might be
        // temporary problems, but it doesn't really matter here. It just means it could take a few more errors/successes to go for maximum/zero delay.
        public int ErrorCount { get; private set; }

        public void OnSuccess()
        {
            if (this.ErrorCount > 0)
            {
                this.ErrorCount >>= 1;
            }
        }

        public void OnFailure()
        {
            if (this.ErrorCount - 1 < this.maxRetryExponent)
            {
                this.ErrorCount++;
            }
        }

        public TimeSpan GetDelay()
        {
            if (this.ErrorCount <= 1)
            {
                return TimeSpan.Zero;
            }

            lock (Random)
            {
                // Random isn't thread safe, concurrent calls can corrupt it's internal state, so we lock here.
                return TimeSpan.FromSeconds(1 << (this.ErrorCount - 1)) * Random.NextDouble();
            }
        }
    }
}
