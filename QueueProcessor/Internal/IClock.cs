using System;

namespace QueueProcessor.Internal
{
    public interface IClock
    {
        DateTime GetCurrentInstant();
    }
}
