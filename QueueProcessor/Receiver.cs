using QueueProcessor.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class Receiver<TMessage> : IReceiver<TMessage>
    {
        private readonly Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func;
        private readonly ILogger<TMessage> logger;
        private readonly IPollingStrategy pollingStrategy;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly ConcurrentTaskRunner runner;
        private readonly ReceiverLimiter limiter;

        public Receiver(
            string name,
            Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func,
            ILogger<TMessage>? logger = null,
            IPollingStrategy? pollingStrategy = null,
            ICircuitBreaker? circuitBreaker = null,
            int concurrency = 1,
            int inflightMessageLimit = int.MaxValue)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.pollingStrategy = pollingStrategy ?? new IntervalPollingStrategy(int.MaxValue, TimeSpan.FromSeconds(5.0));
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(0.5, TimeSpan.FromSeconds(5.0), 10, TimeSpan.FromSeconds(10.0));
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync, e => this.logger.LogException(this.Name, e));
            this.limiter = new ReceiverLimiter(inflightMessageLimit);
        }

        public event Action<IEnumerable<TMessage>>? Received;

        public string Name { get; }

        public void Start() => this.runner.Start();

        public Task StopAsync() => this.runner.StopAsync();

        public void OnClosed(IEnumerable<TMessage> messages)
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            this.limiter.OnClosed(messages.Count());
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            int previousBatchSize = 0;
            while (true)
            {
                TimeSpan normalDelay = this.pollingStrategy.GetDelay(previousBatchSize);
                TimeSpan? errorDelay = this.circuitBreaker.GetDelay();
                await Task.Delay(errorDelay ?? normalDelay, cancellationToken).ConfigureAwait(false);
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
