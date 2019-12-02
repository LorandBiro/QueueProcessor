using System;
using System.Collections.Generic;
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
        private readonly IRetryPolicy retryPolicy;
        private readonly List<BackgroundProcess> backgroundProcesses = new List<BackgroundProcess>();

        public ReceiverService(
            MessageReceiver<TMessage> receiver,
            Func<TMessage, IProcessorService<TMessage>> router,
            ILogger? logger = null,
            IRetryPolicy? retryPolicy = null,
            int concurrency = 1)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Cuncurrency must be at least 1.");
            }

            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.logger = logger ?? new DebugLogger();
            this.retryPolicy = retryPolicy ?? new DefaultRetryPolicy(5);
            for (int i = 0; i < concurrency; i++)
            {
                this.backgroundProcesses.Add(new BackgroundProcess(this.logger, this.MainAsync));
            }
        }

        public void Start()
        {
            this.backgroundProcesses.ForEach(x => x.Start());
        }

        public async Task StopAsync()
        {
            await Task.WhenAll(this.backgroundProcesses.Select(x => x.StopAsync())).ConfigureAwait(false);
        }

        private async Task MainAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<TMessage> messages = await this.receiver(cancellationToken).ConfigureAwait(false);
            if (messages.Count == 0)
            {
                return;
            }

            foreach (IGrouping<IProcessorService<TMessage>, TMessage> group in messages.GroupBy(x => this.router(x), x => x))
            {
                group.Key.Enqueue(group);
            }
        }
    }
}
