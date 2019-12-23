using QueueProcessor.Processing;
using System;

namespace QueueProcessor.Processing
{
    public abstract class Op
    {
        public static readonly Op Close = new CloseOp();
        public static readonly RetryOp InstantRetry = new RetryOp(TimeSpan.Zero);

        public static Op TransferTo<TMessage>(IProcessor<TMessage> processor) => new TransferOp<TMessage>(processor);
        public static Op Retry(TimeSpan delay) => new RetryOp(delay);
    }

    public sealed class TransferOp<TMessage> : Op
    {
        public TransferOp(IProcessor<TMessage> processor)
        {
            this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public IProcessor<TMessage> Processor { get; }
        public override string ToString() => "Transfer to " + this.Processor.Name;
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

        public override string ToString() => this.Delay == TimeSpan.Zero ? "Retry" : "Retry in " + this.Delay;
    }

    public sealed class CloseOp : Op
    {
        public override string ToString() => "Close";
    }
}
