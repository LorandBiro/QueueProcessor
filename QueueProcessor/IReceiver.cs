using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IReceiver<TMessage>
    {
        void OnClosed(IEnumerable<TMessage> messages);

        void Start();

        Task StopAsync();
    }
}
