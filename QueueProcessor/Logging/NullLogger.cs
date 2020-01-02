using System;
using QueueProcessor.Processing;

namespace QueueProcessor.Logging
{
    public sealed class NullLogger<TMessage> : ILogger<TMessage>
    {
        public static readonly NullLogger<TMessage> Instance = new NullLogger<TMessage>();

        public void LogException(string service, Exception exception) { }

        public void LogMessageClosed(TMessage message) { }

        public void LogMessageProcessed(string service, TMessage message, Result result, IProcessor<TMessage>? nextProcessor) { }

        public void LogMessageReceived(TMessage message, IProcessor<TMessage> nextProcessor) { }
    }
}
