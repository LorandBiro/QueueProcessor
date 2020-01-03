namespace QueueProcessor.Logging
{
    public interface ITracer<TMessage>
    {
        IOperation<TMessage> StartOperation(string name);
    }
}
