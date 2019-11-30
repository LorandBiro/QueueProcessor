using System;

namespace QueueProcessor
{
    public interface IRetryPolicy
    {
        void OnSuccess();

        void OnFailure();

        TimeSpan GetDelay();
    }
}
