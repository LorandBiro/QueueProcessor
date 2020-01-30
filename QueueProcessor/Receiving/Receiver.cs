using QueueProcessor.Logging;
using QueueProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Receiving
{
    public sealed class Receiver<TMessage> : IReceiver<TMessage>
    {
        private readonly Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func;
        private readonly IReceiverStrategy strategy;
        private readonly ConcurrentTaskRunner runner;
        private readonly ITracer<TMessage> tracer;

        public Receiver(
            string name,
            Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> func,
            IReceiverStrategy strategy,
            int concurrency = 1,
            ITracer<TMessage>? tracer = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync, e => this.tracer.TrackException(this.Name, e));
            this.tracer = tracer ?? NullTracer<TMessage>.Instance;
        }

        public event Action<IReadOnlyCollection<TMessage>>? Received;

        public string Name { get; }

        public void Start() => this.runner.Start();

        public void OnInflightCountChanged(int count) => this.strategy.OnInflightCountChanged(count);

        public ValueTask DisposeAsync() => this.runner.DisposeAsync();

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await this.strategy.WaitAsync(cancellationToken).ConfigureAwait(false);

                using IOperation<TMessage> operation = this.tracer.StartOperation(this.Name);
                try
                {
                    IReadOnlyCollection<TMessage> batch = await this.func(cancellationToken).ConfigureAwait(false);
                    operation.OnReceived(batch);

                    this.Received?.Invoke(batch);
                    this.strategy.OnSuccess(batch.Count);
                }
                catch (Exception exception)
                {
                    operation.OnException(exception);
                    if (exception is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                    {
                        throw;
                    }

                    this.strategy.OnFailure(exception);
                }
            }
        }
    }
}
