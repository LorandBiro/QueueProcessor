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
        private readonly ILogger logger;
        private readonly IReceiverStrategy receiverStrategy;
        private readonly IRetryPolicy retryPolicy;
        private readonly List<BackgroundProcess> backgroundProcesses = new List<BackgroundProcess>();
        private readonly ReceiverLimiter limiter;

        public ReceiverService(
            MessageReceiver<TMessage> receiver,
            Func<TMessage, IProcessorService<TMessage>> router,
            ILogger? logger = null,
            IReceiverStrategy? receiverStrategy = null,
            IRetryPolicy? retryPolicy = null,
            int concurrency = 1,
            int inflightMessageLimit = int.MaxValue)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Concurrency must be at least 1.");
            }

            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.logger = logger ?? new DebugLogger();
            this.receiverStrategy = receiverStrategy ?? new ConstantRateRandomReceiverStrategy(TimeSpan.FromSeconds(5.0));
            this.retryPolicy = retryPolicy ?? new DefaultRetryPolicy(5);
            for (int i = 0; i < concurrency; i++)
            {
                this.backgroundProcesses.Add(new BackgroundProcess(this.logger, this.MainAsync));
            }

            this.limiter = new ReceiverLimiter(inflightMessageLimit);
        }

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
                    foreach (IGrouping<IProcessorService<TMessage>, TMessage> group in batch.GroupBy(x => this.router(x), x => x))
                    {
                        group.Key.Enqueue(group);
                    }

                    this.retryPolicy.OnSuccess();
                    previousBatchSize = batch.Count;
                }
                catch (Exception e)
                {
                    if (e is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                    {
                        throw;
                    }

                    this.retryPolicy.OnFailure();
                    this.logger.LogError(e);
                }
            }
        }
    }
}
