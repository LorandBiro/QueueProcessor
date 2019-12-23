using System;

namespace QueueProcessor.Utils
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
