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
        private readonly Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> receiver;
        private readonly Func<TMessage, IProcessor<TMessage>> router;
        private readonly ILogger<TMessage> logger;
        private readonly IReceiverStrategy receiverStrategy;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly ConcurrentTaskRunner runner;
        private readonly ReceiverLimiter limiter;

        public Receiver(
            string name,
            Func<CancellationToken, Task<IReadOnlyCollection<TMessage>>> receiver,
            Func<TMessage, IProcessor<TMessage>> router,
            ILogger<TMessage>? logger = null,
            IReceiverStrategy? receiverStrategy = null,
            ICircuitBreaker? circuitBreaker = null,
            int concurrency = 1,
            int inflightMessageLimit = int.MaxValue)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.receiverStrategy = receiverStrategy ?? new FixedIntervalReceiverStrategy(new Clock(), TimeSpan.FromSeconds(5.0));
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(5);
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync);
            this.runner.Exception += (sender, e) => this.logger.LogServiceException(this.Name, e.Exception);
            this.limiter = new ReceiverLimiter(inflightMessageLimit);
        }

        public string Name { get; }

        public void OnClosed(IEnumerable<TMessage> messages)
        {
            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            this.limiter.OnClosed(messages.Count());
        }

        public void Start() => this.runner.Start();

        public Task StopAsync() => this.runner.StopAsync();

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            int previousBatchSize = 0;
            while (true)
            {
                TimeSpan normalDelay = this.receiverStrategy.GetDelay(previousBatchSize);
                TimeSpan errorDelay = this.circuitBreaker.GetDelay();
                await Task.Delay(errorDelay > normalDelay ? errorDelay : normalDelay, cancellationToken).ConfigureAwait(false);
                await this.limiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    IReadOnlyCollection<TMessage> batch = await this.receiver(cancellationToken).ConfigureAwait(false);
                    var messagesWithRoutes = batch.Select(x => new { Message = x, Processor = this.router(x) }).ToList();
                    foreach (var item in messagesWithRoutes)
                    {
                        this.logger.LogMessageReceived(this.Name, item.Message, item.Processor);
                    }

                    foreach (IGrouping<IProcessor<TMessage>, TMessage> group in messagesWithRoutes.GroupBy(x => x.Processor, x => x.Message))
                    {
                        group.Key.Enqueue(group);
                    }

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
