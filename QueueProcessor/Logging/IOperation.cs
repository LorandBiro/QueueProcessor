using QueueProcessor.Processing;
using System;
using System.Collections.Generic;

namespace QueueProcessor.Logging
{
    public interface IOperation<TMessage> : IDisposable
    {
        void OnReceived(IReadOnlyCollection<TMessage> messages);

        void OnProcessorResult(IReadOnlyList<Job<TMessage>> jobs);

        void OnException(Exception exception);
    }
}
