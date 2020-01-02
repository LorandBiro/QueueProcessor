using QueueProcessor.Logging;
using QueueProcessor.Processing;
using QueueProcessor.Receiving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class QueueService<TMessage>
    {
        private readonly object countLocker = new object();

        private readonly ILogger<TMessage> logger;
        private readonly IReceiver<TMessage>? receiver;
        private readonly Func<TMessage, IProcessor<TMessage>> router;
        private readonly IReadOnlyList<IProcessor<TMessage>> processors;

        public QueueService(ILogger<TMessage> logger, IReceiver<TMessage>? receiver, Func<TMessage, IProcessor<TMessage>> router, params IProcessor<TMessage>[] processors)
        {
            if (processors is null)
            {
                throw new ArgumentNullException(nameof(processors));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.receiver = receiver;
            if (this.receiver != null)
            {
                this.receiver.Received += this.OnReceived;
            }

            this.router = router ?? throw new ArgumentNullException(nameof(router));
            this.processors = processors as IReadOnlyList<IProcessor<TMessage>> ?? processors.ToList();
            foreach (IProcessor<TMessage> processor in this.processors)
            {
                processor.Closed += this.OnClosed;
            }

            if (this.processors.Count == 0)
            {
                throw new ArgumentException("At least 1 processor must be provided.", nameof(processors));
            }
        }

        public int Count { get; private set; }

        public void Start()
        {
            foreach (IProcessor<TMessage> processor in this.processors)
            {
                processor.Start();
            }

            this.receiver?.Start();
        }

        public async Task StopAsync()
        {
            if (this.receiver != null)
            {
                await this.receiver.StopAsync().ConfigureAwait(false);
            }

            foreach (IProcessor<TMessage> processor in this.processors)
            {
                await processor.StopAsync().ConfigureAwait(false);
            }
        }

        public void Enqueue(params TMessage[] batch) => this.Enqueue((IEnumerable<TMessage>)batch);

        public void Enqueue(IEnumerable<TMessage> batch)
        {
            if (batch is null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            this.OnReceived(batch as IReadOnlyCollection<TMessage> ?? batch.ToList());
        }

        private void OnReceived(IReadOnlyCollection<TMessage> batch)
        {
            lock (this.countLocker)
            {
                this.Count += batch.Count;
                this.receiver?.OnInflightCountChanged(this.Count);
            }

            var messagesWithRoutes = batch.Select(x => new { Message = x, Processor = this.router(x) }).ToList();
            foreach (var item in messagesWithRoutes)
            {
                this.logger.LogMessageReceived(item.Message, item.Processor);
            }

            foreach (IGrouping<IProcessor<TMessage>, TMessage> group in messagesWithRoutes.GroupBy(x => x.Processor, x => x.Message))
            {
                group.Key.Enqueue(group);
            }
        }

        private void OnClosed(IReadOnlyCollection<TMessage> batch)
        {
            lock (this.countLocker)
            {
                this.Count -= batch.Count;
                this.receiver?.OnInflightCountChanged(this.Count);
            }

            foreach (TMessage message in batch)
            {
                this.logger.LogMessageClosed(message);
            }
        }
    }
}
