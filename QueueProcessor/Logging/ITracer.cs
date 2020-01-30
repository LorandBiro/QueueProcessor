using System;

namespace QueueProcessor.Logging
{
    public interface ITracer<TMessage>
    {
        IOperation<TMessage> StartOperation(string name);

        void TrackException(string service, Exception exception);
    }
}
