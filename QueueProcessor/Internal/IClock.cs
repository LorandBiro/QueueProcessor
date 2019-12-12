using System;

namespace QueueProcessor.Internal
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
