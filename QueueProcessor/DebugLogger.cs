using System;
using System.Diagnostics;

namespace QueueProcessor
{
    public sealed class DebugLogger : ILogger
    {
        public void LogError(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Debug.Fail(exception.ToString());
        }

        public void LogError(Exception exception, string message)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Debug.Fail(message, exception.ToString());
        }
    }
}
