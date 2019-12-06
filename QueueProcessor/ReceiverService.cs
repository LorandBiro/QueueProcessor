using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public delegate Task<IReadOnlyCollection<TMessage>> MessageReceiver<TMessage>(CancellationToken cancellationToken);

    public sealed class ReceiverService<TMessage> : IReceiverService<TMessage>
    {
        private readonly MessageReceiver<TMessage> receiver;
        private readonly Func<TMessage, IProcessorService<TMessage>> router;
        private readonly ILogger<TMessage> logger;
        private readonly IReceiverStrategy receiverStrategy;
        private readonly IRetryPolicy retryPolicy;
        private readonly List<TaskRunner> backgroundProcesses = new List<TaskRunner>();
        private readonly ReceiverLimiter limiter;

        public ReceiverService(
            string name,
            MessageReceiver<TMessage> receiver,
            Func<TMessage, IProcessorService<TMessage>> router,
            ILogger<TMessage>? logger = null,
            IReceiverStrategy? receiverStrategy = null,
            IRetryPolicy? retryPolicy = null,
            int concurrency = 1,
            int inflightMessageLimit = int.MaxValue)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Concurrency must be at least 1.");
            }

            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.receiverStrategy = receiverStrategy ?? new ConstantRateRandomReceiverStrategy(TimeSpan.FromSeconds(5.0));
            this.retryPolicy = retryPolicy ?? new DefaultRetryPolicy(5);
            for (int i = 0; i < concurrency; i++)
            {
                TaskRunner runner = new TaskRunner(this.MainAsync);
                runner.Exception += (sender, e) => this.logger.LogServiceException(this.Name, e.Exception);
                this.backgroundProcesses.Add(runner);
            }

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

        public void Start() => this.backgroundProcesses.ForEach(x => x.Start());

        public Task StopAsync() => Task.WhenAll(this.backgroundProcesses.Select(x => x.StopAsync()));

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            int previousBatchSize = 0;
            while (true)
            {
                TimeSpan normalDelay = this.receiverStrategy.GetDelay(previousBatchSize);
                TimeSpan errorDelay = this.retryPolicy.GetDelay();
                await Task.Delay(errorDelay > normalDelay ? errorDelay : normalDelay, cancellationToken).ConfigureAwait(false);
                await this.limiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    IReadOnlyCollection<TMessage> batch = await this.receiver(cancellationToken).ConfigureAwait(false);
                    foreach (TMessage message in batch)
                    {
                        this.logger.LogMessageReceived(this.Name, message);
                    }

                    foreach (IGrouping<IProcessorService<TMessage>, TMessage> group in batch.GroupBy(x => this.router(x), x => x))
                    {
                        group.Key.Enqueue(group);
                    }

                    this.retryPolicy.OnSuccess();
                    previousBatchSize = batch.Count;
                }
                catch (Exception exception)
                {
                    if (exception is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                    {
                        throw;
                    }

                    this.retryPolicy.OnFailure();
                }
            }
        }
    }
}
