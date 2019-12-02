using System.Threading.Tasks;

namespace QueueProcessor
{
    public interface IReceiverService<TMessage>
    {
        void Start();

        Task StopAsync();
    }
}
