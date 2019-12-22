using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IReceiver<TMessage>
    {
        event Action<IEnumerable<TMessage>> Received;

        void OnClosed(IEnumerable<TMessage> messages);

        void Start();

        Task StopAsync();
    }
}
