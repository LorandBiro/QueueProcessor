using System;

namespace QueueProcessor.Utils
{
    public sealed class ClockStub : IClock
    {
        public static readonly DateTime Zero = new DateTime(0, DateTimeKind.Utc);

        public DateTime Now { get; set; } = Zero;

        public void Add(TimeSpan ts) => this.Now = this.Now.Add(ts);
        public void AddSeconds(double seconds) => this.Now = this.Now.AddSeconds(seconds);

        public void SetSeconds(double seconds) => this.Now = Zero.AddSeconds(seconds);
    }
}
