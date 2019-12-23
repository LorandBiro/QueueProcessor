using System;

namespace QueueProcessor.CircuitBreaking
{
    public interface ICircuitBreaker
    {
        void OnSuccess();

        void OnFailure();

        TimeSpan? GetDelay();
    }
}
