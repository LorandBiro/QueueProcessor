using System;

namespace QueueProcessor
{
    public interface ICircuitBreaker
    {
        void OnSuccess();

        void OnFailure();

        TimeSpan? GetDelay();
    }
}
