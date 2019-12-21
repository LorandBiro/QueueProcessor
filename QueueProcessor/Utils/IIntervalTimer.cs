using System;

namespace QueueProcessor.Utils
{
    public interface IIntervalTimer
    {
        TimeSpan GetDelay();
    }
}