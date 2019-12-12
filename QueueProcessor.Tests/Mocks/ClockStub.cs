using QueueProcessor.Internal;
using System;

namespace QueueProcessor.Mocks
{
    public sealed class ClockStub : IClock
    {
        public DateTime Now { get; set; }
    }
}
