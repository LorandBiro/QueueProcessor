using System;

namespace QueueProcessor
{
    public abstract class Op
    {
        public static readonly Op Close = new CloseOp();
        public static readonly RetryOp InstantRetry = new RetryOp(TimeSpan.Zero);

        public static Op TransferTo<TMessage>(IProcessorService<TMessage> processor) => new TransferOp<TMessage>(processor);
        public static Op Retry(TimeSpan delay) => new RetryOp(delay);
    }

    public sealed class TransferOp<TMessage> : Op
    {
        public TransferOp(IProcessorService<TMessage> processor)
        {
            this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public IProcessorService<TMessage> Processor { get; }
    }

    public sealed class RetryOp : Op
    {
        public RetryOp(TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be non-negative.");
            }

            this.Delay = delay;
        }

        public TimeSpan Delay { get; }
    }

    public sealed class CloseOp : Op { }
}
