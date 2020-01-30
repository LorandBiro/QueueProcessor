using System;

namespace QueueProcessor.Logging
{
    public sealed class NullTracer<TMessage> : ITracer<TMessage>
    {
        public static readonly NullTracer<TMessage> Instance = new NullTracer<TMessage>();

        public IOperation<TMessage> StartOperation(string name) => NullOperation<TMessage>.Instance;

        public void TrackException(string service, Exception exception) { }
    }
}
