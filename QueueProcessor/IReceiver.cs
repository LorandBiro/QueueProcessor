using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IReceiver<TMessage>
    {
        event Action<IReadOnlyCollection<TMessage>> Received;

        void Start();

        Task StopAsync();

        void OnMessageCountChanged(int count);
    }
}
