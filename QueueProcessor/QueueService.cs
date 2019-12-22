using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class QueueService<TMessage>
    {
        private readonly object locker = new object();

        private readonly ILogger<TMessage> logger;
        private readonly int limit;
        private readonly IReceiver<TMessage> receiver;
        private readonly Func<TMessage, IProcessor<TMessage>> router;
        private readonly IReadOnlyList<IProcessor<TMessage>> processors;

        public QueueService(ILogger<TMessage> logger, int limit, IReceiver<TMessage> receiver, Func<TMessage, IProcessor<TMessage>> router, params IProcessor<TMessage>[] processors)
        {
            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "The limit must be at least 1.");
            }

            if (processors is null)
            {
                throw new ArgumentNullException(nameof(processors));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.limit = limit;
            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.receiver.Received += this.OnReceived;
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

            this.receiver.Start();
        }

        public async Task StopAsync()
        {
            await this.receiver.StopAsync().ConfigureAwait(false);
            foreach (IProcessor<TMessage> processor in this.processors)
            {
                await processor.StopAsync().ConfigureAwait(false);
            }
        }

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
            lock (this.locker)
            {
                this.Count += batch.Count;
                if (this.Count >= this.limit && this.receiver.IsEnabled)
                {
                    this.receiver.Disable();
                }
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
            lock (this.locker)
            {
                this.Count -= batch.Count;
                if (!this.receiver.IsEnabled && this.Count < this.limit)
                {
                    this.receiver.Enable();
                }
            }
        }
    }
}
