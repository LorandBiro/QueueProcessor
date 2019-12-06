namespace QueueProcessor
{
    public sealed class WorkItem<TMessage>
    {
        public WorkItem(TMessage message)
        {
            this.Message = message;
        }

        public TMessage Message { get; }
        public Result Result { get; }

        public int ErrorCount { get; private set; }
    }
}
