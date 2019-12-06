using System;
using System.Diagnostics;

namespace QueueProcessor
{
    public sealed class DebugLogger<TMessage> : ILogger<TMessage>
    {
        public void LogMessageFailed(string service, TMessage message, Result result, Op op)
        {
            Debug.Fail($"{service}: {message} {result} => {op}", result.Exception?.ToString());
        }

        public void LogMessageProcessed(string service, TMessage message, Result result, Op op)
        {
            Debug.WriteLine($"{service}: {message} {result} => {op}");
        }

        public void LogMessageReceived(string service, TMessage message)
        {
            Debug.WriteLine($"{service}: {message} received");
        }

        public void LogServiceException(string service, Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Debug.Fail($"{service}: unexpected exception", exception.ToString());
        }
    }
}
