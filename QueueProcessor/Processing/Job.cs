namespace QueueProcessor.Processing
{
    public sealed class Job<TMessage>
    {
        public Job(TMessage message)
        {
            this.Message = message;
        }

        public TMessage Message { get; }
        public Result Result { get; private set; }

        public void SetResult(Result result)
        {
            this.Result = result;
        }
    }
}
