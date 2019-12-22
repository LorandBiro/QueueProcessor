using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IReceiver<TMessage>
    {
        event Action<IReadOnlyCollection<TMessage>> Received;

        bool IsEnabled { get; }

        void Start();

        Task StopAsync();

        void Enable();

        void Disable();
    }
}
