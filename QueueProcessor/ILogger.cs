using System;

namespace QueueProcessor
{
    public interface ILogger
    {
        void LogError(Exception exception);
        void LogError(Exception exception, string message);
    }
}
