using System;

namespace QueueProcessor
{
    public interface IRetryPolicy
    {
        int ErrorCount { get; }

        void OnSuccess();

        void OnFailure();

        TimeSpan GetDelay();
    }
}
