using System;
using System.Collections.Generic;
using QueueProcessor.Processing;

namespace QueueProcessor.Logging
{
    public sealed class NullOperation<TMessage> : IOperation<TMessage>
    {
        public static readonly NullOperation<TMessage> Instance = new NullOperation<TMessage>();

        public void OnReceived(IReadOnlyCollection<TMessage> messages) { }

        public void OnProcessorResult(IReadOnlyList<Job<TMessage>> jobs) { }

        public void OnException(Exception exception) { }

        public void Dispose() { }
    }
}
