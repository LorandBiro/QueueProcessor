using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor.Receiving
{
    public interface IReceiver<TMessage>
    {
        event Action<IReadOnlyCollection<TMessage>> Received;

        void Start();

        Task StopAsync();

        void OnInflightCountChanged(int count);
    }
}
