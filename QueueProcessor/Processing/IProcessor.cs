using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor.Processing
{
    public interface IProcessor<TMessage> : IAsyncDisposable
    {
        string Name { get; }

        void Start();

        void Enqueue(IEnumerable<TMessage> messages);

        event Action<IReadOnlyCollection<TMessage>> Closed;
    }
}
