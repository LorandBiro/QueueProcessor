using System;

namespace QueueProcessor.Timers
{
    public class TimerStub : ITimer
    {
        public TimeSpan Delay { get; set; }

        public TimeSpan GetDelay() => this.Delay;
    }
}
