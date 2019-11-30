using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor.Internals
{
    public interface IProcessorService<TMessage>
    {
        void Start();

        Task StopAsync();

        void Enqueue(IEnumerable<TMessage> messages);

        event MessageEventHandler<TMessage> Processed;
    }
}
