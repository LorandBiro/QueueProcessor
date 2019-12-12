using System;

namespace QueueProcessor
{
    public interface IReceiverStrategy
    {
        TimeSpan GetDelay(int batchSize);
    }
}
