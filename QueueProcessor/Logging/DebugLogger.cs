using QueueProcessor.Processing;
using System;
using System.Diagnostics;

namespace QueueProcessor.Logging
{
    public sealed class DebugLogger<TMessage> : ILogger<TMessage>
    {
        public void LogMessageProcessed(string service, TMessage message, Result result, IProcessor<TMessage>? nextProcessor)
        {
            if (result.IsError)
            {
                Debug.Fail($"{service}: {message} {result} => {nextProcessor?.Name}", result.Exception?.ToString());
            }
            else
            {
                Debug.WriteLine($"{service}: {message} {result} => {nextProcessor?.Name}");
            }
        }

        public void LogMessageReceived(TMessage message, IProcessor<TMessage> nextProcessor)
        {
            if (nextProcessor is null)
            {
                throw new ArgumentNullException(nameof(nextProcessor));
            }

            Debug.WriteLine($"{message} Received => {nextProcessor.Name}");
        }

        public void LogMessageClosed(TMessage message)
        {
            Debug.WriteLine($"{message} Closed");
        }

        public void LogException(string service, Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Debug.Fail($"{service}: unexpected exception", exception.ToString());
        }
    }
}
