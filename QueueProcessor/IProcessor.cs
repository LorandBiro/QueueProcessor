using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IProcessor<TMessage>
    {
        string Name { get; }

        void Start();

        Task StopAsync();

        void Enqueue(IEnumerable<TMessage> messages);

        event Action<IEnumerable<TMessage>> Closed;
    }
}
