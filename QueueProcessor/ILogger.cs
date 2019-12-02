using System;

namespace QueueProcessor
{
    public interface ILogger
    {
        void Trace(string message);
        void LogError(Exception exception);
        void LogError(Exception exception, string message);
    }
}
