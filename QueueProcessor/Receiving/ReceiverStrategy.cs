using QueueProcessor.CircuitBreaking;
using QueueProcessor.Timers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Receiving
{
    public sealed class ReceiverStrategy : IReceiverStrategy
    {
        private readonly ITimer pollingStrategy;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly int burstBatchLimit;
        private readonly int inflightLimit;
        private readonly ReceiverLimiter limiter = new ReceiverLimiter();
        private bool burst;

        public ReceiverStrategy(
            ITimer? pollingStrategy = null,
            ICircuitBreaker? circuitBreaker = null,
            int burstBatchLimit = int.MaxValue,
            int inflightLimit = int.MaxValue)
        {
            if (inflightLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(inflightLimit), inflightLimit, "The limit must be at least 1.");
            }

            this.pollingStrategy = pollingStrategy ?? new IntervalTimer(TimeSpan.FromSeconds(1.0));
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(0.5, new IntervalTimer(TimeSpan.FromSeconds(5.0)), 10, TimeSpan.FromSeconds(10.0));
            this.burstBatchLimit = burstBatchLimit;
            this.inflightLimit = inflightLimit;
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            TimeSpan delay = this.circuitBreaker.GetDelay() ?? (burst ? TimeSpan.Zero : this.pollingStrategy.GetDelay());
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await this.limiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void OnInflightCountChanged(int count)
        {
            if (this.limiter.IsEnabled)
            {
                if (count < this.inflightLimit)
                {
                    this.limiter.Disable();
                }
            }
            else
            {
                if (count >= this.inflightLimit)
                {
                    this.limiter.Enable();
                }
            }
        }

        public void OnSuccess(int batchSize)
        {
            burst = batchSize >= this.burstBatchLimit;
            this.circuitBreaker.OnSuccess();
        }

        public void OnFailure(Exception exception)
        {
            this.circuitBreaker.OnFailure();
        }
    }
}
