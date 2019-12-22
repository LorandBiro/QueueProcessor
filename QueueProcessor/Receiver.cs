using QueueProcessor.Internal;
using QueueProcessor.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class Receiver<TMessage> : IReceiver<TMessage>
    {
        private readonly Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func;
        private readonly ILogger<TMessage> logger;
        private readonly ITimer pollingStrategy;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly int maxBatchSize;
        private readonly ConcurrentTaskRunner runner;

        private readonly ReceiverLimiter limiter = new ReceiverLimiter();

        public Receiver(
            string name,
            Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func,
            ILogger<TMessage>? logger = null,
            ITimer? pollingStrategy = null,
            ICircuitBreaker? circuitBreaker = null,
            int concurrency = 1,
            int maxBatchSize = 1)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.pollingStrategy = pollingStrategy ?? new IntervalTimer(TimeSpan.FromSeconds(1.0));
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(0.5, new IntervalTimer(TimeSpan.FromSeconds(5.0)), 10, TimeSpan.FromSeconds(10.0));
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync, e => this.logger.LogException(this.Name, e));
            this.maxBatchSize = maxBatchSize;
        }

        public event Action<IReadOnlyCollection<TMessage>>? Received;

        public string Name { get; }

        public bool IsEnabled => !this.limiter.IsEnabled;

        public void Start() => this.runner.Start();

        public Task StopAsync() => this.runner.StopAsync();

        public void Enable() => this.limiter.Disable();

        public void Disable() => this.limiter.Enable();

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            int previousBatchSize = 0;
            while (true)
            {
                TimeSpan delay = this.circuitBreaker.GetDelay() ?? (previousBatchSize < this.maxBatchSize ? this.pollingStrategy.GetDelay() : TimeSpan.Zero);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                await this.limiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    IReadOnlyCollection<TMessage> batch = await this.func(cancellationToken).ConfigureAwait(false);
                    this.Received?.Invoke(batch);
                    this.circuitBreaker.OnSuccess();
                    previousBatchSize = batch.Count;
                }
                catch (Exception exception)
                {
                    if (exception is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                    {
                        throw;
                    }

                    this.circuitBreaker.OnFailure();
                }
            }
        }
    }
}
