using System;

namespace QueueProcessor
{
    public interface ICircuitBreaker
    {
        int ErrorCount { get; }

        void OnSuccess();

        void OnFailure();

        TimeSpan GetDelay();
    }
}
