using QueueProcessor.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class QueueService<TMessage>
    {
        private readonly IReceiverService<TMessage> receiver;
        private readonly IReadOnlyList<IProcessorService<TMessage>> processors;

        public QueueService(IReceiverService<TMessage> receiver, params IProcessorService<TMessage>[] processors)
            : this(receiver, (IEnumerable<IProcessorService<TMessage>>)processors) { }

        public QueueService(IReceiverService<TMessage> receiver, IEnumerable<IProcessorService<TMessage>> processors)
        {
            if (processors is null)
            {
                throw new ArgumentNullException(nameof(processors));
            }

            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            this.processors = processors as IReadOnlyList<IProcessorService<TMessage>> ?? processors.ToList();
            if (this.processors.Count == 0)
            {
                throw new ArgumentException("At least 1 processor must be provided.", nameof(processors));
            }
        }

        public void Start()
        {
            foreach (IProcessorService<TMessage> processor in this.processors)
            {
                processor.Start();
            }

            this.receiver.Start();
        }

        public async Task StopAsync()
        {
            await this.receiver.StopAsync().ConfigureAwait(false);
            foreach (IProcessorService<TMessage> processor in this.processors)
            {
                await processor.StopAsync().ConfigureAwait(false);
            }
        }
    }
}
