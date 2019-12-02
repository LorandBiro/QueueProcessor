using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IProcessorService<TMessage>
    {
        void Start();

        Task StopAsync();

        void Enqueue(IEnumerable<TMessage> messages);
    }
}
