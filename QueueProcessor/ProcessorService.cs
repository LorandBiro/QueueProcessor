using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public delegate Task MessageProcessor<TMessage>(IReadOnlyList<WorkItem<TMessage>> workItems, CancellationToken cancellationToken);

    public sealed class ProcessorService<TMessage> : IProcessorService<TMessage>
    {
        private static readonly Action<WorkItem<TMessage>> DefaultOnSuccess = workItem => workItem.Close();
        private static readonly Action<WorkItem<TMessage>, Exception> DefaultOnFailure = (workItem, exception) => workItem.Retry(result: Result.Error(exception));

        public ProcessorService(
            string name,
            MessageProcessor<TMessage> processor,
            int concurrency = 1,
            int maxBatchSize = 0,
            TimeSpan maxBatchDelay = default,
            Action<WorkItem<TMessage>>? onSuccess = null,
            Action<WorkItem<TMessage>, Exception>? onFailure = null,
            IRetryPolicy? retryPolicy = null)
        {
        }

        public void Enqueue(IEnumerable<TMessage> messages)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }
    }
}
