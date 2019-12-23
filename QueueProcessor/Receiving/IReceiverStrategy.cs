using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Receiving
{
    public interface IReceiverStrategy
    {
        Task WaitAsync(CancellationToken cancellationToken);

        void OnInflightCountChanged(int count);

        void OnSuccess(int batchSize);

        void OnFailure(Exception exception);
    }
}
