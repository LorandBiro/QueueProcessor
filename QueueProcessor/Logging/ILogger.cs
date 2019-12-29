using QueueProcessor.Processing;
using System;

namespace QueueProcessor.Logging
{
    public interface ILogger<TMessage>
    {
        void LogMessageReceived(TMessage message, IProcessor<TMessage> nextProcessor);
        void LogMessageClosed(TMessage message);

        void LogMessageProcessed(string service, TMessage message, Result result, IProcessor<TMessage>? nextProcessor);

        void LogException(string service, Exception exception);
    }
}
