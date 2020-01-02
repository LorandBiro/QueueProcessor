using System;
using System.Collections.Generic;

namespace QueueProcessor.Receiving
{
    public interface IReceiver<TMessage> : IAsyncDisposable
    {
        event Action<IReadOnlyCollection<TMessage>> Received;

        void Start();

        void OnInflightCountChanged(int count);
    }
}
