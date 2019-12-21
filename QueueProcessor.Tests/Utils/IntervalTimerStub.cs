using System;

namespace QueueProcessor.Utils
{
    public class IntervalTimerStub : IIntervalTimer
    {
        public TimeSpan Delay { get; set; }

        public TimeSpan GetDelay() => this.Delay;
    }
}
