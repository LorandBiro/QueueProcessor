using System;

namespace QueueProcessor.Timers
{
    public sealed class ConstantTimer : ITimer
    {
        private readonly TimeSpan delay;

        public ConstantTimer(TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be at least 0.");
            }

            this.delay = delay;
        }

        public TimeSpan GetDelay() => delay;
    }
}
