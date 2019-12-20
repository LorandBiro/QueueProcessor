using System;

namespace QueueProcessor.Internal
{
    public class FailureRateCalculator : IFailureRateCalculator
    {
        private readonly IClock clock;
        private readonly Bucket[] buckets;
        private readonly TimeSpan bucketDuration;
        private int currentBucketIndex;
        private DateTime currentBucketTimestamp;

        public FailureRateCalculator(int bucketCount, TimeSpan bucketDuration)
            : this(bucketCount, bucketDuration, Clock.Instance) { }

        public FailureRateCalculator(int bucketCount, TimeSpan bucketDuration, IClock clock)
        {
            if (bucketCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bucketCount), bucketCount, "Bucket count must be larger than zero.");
            }

            if (bucketDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(bucketDuration), bucketDuration, "Bucket duration must be larger than zero.");
            }

            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.buckets = new Bucket[bucketCount];
            this.bucketDuration = bucketDuration;

            this.currentBucketTimestamp = this.clock.Now;
        }

        public void OnSuccess()
        {
            lock (this.buckets)
            {
                this.AdvanceBuckets();
                this.buckets[this.currentBucketIndex].SuccessCount++;
            }
        }

        public void OnFailure()
        {
            lock (this.buckets)
            {
                this.AdvanceBuckets();
                this.buckets[this.currentBucketIndex].FailureCount++;
            }
        }

        public double GetFailureRate()
        {
            lock (this.buckets)
            {
                this.AdvanceBuckets();
                int successCount = 0;
                int failureCount = 0;
                for (int i = 0; i < this.buckets.Length; i++)
                {
                    successCount += this.buckets[i].SuccessCount;
                    failureCount += this.buckets[i].FailureCount;
                }

                int sum = successCount + failureCount;
                return sum == 0 ? 0.0 : failureCount / (double)sum;
            }
        }

        private void AdvanceBuckets()
        {
            DateTime now = this.clock.Now;
            TimeSpan diff = now - this.currentBucketTimestamp;
            while (diff >= this.bucketDuration)
            {
                this.currentBucketIndex = (this.currentBucketIndex + 1) % this.buckets.Length;
                this.currentBucketTimestamp += this.bucketDuration;
                this.buckets[this.currentBucketIndex].Reset();
                diff -= this.bucketDuration;
            }
        }

        private struct Bucket
        {
            public int SuccessCount { get; set; }
            public int FailureCount { get; set; }

            public void Reset()
            {
                this.SuccessCount = 0;
                this.FailureCount = 0;
            }
        }
    }
}
