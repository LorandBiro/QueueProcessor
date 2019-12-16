using System;

namespace QueueProcessor
{
    public interface IPollingStrategy
    {
        TimeSpan GetDelay(int batchSize);
    }
}
