using System;

namespace QueueProcessor
{
    public interface ILogger<TMessage>
    {
        void LogMessageReceived(TMessage message, IProcessor<TMessage> processor);

        void LogMessageProcessed(string service, TMessage message, Result result, Op op);
        void LogMessageFailed(string service, TMessage message, Result result, Op op);

        void LogException(string service, Exception exception);
    }
}
