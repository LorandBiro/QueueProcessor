using System;

namespace QueueProcessor.Timers
{
    public interface ITimer
    {
        TimeSpan GetDelay();
    }
}