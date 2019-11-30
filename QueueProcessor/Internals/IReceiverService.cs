using System.Threading.Tasks;

namespace QueueProcessor.Internals
{
    public interface IReceiverService<TMessage>
    {
        void Start();

        Task StopAsync();
    }
}
