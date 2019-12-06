using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class ReceiverLimiter
    {
        private readonly object locker = new object();

        private readonly int maxCount;
        private TaskCompletionSource<object?>? taskCompletionSource;
        private CancellationTokenRegistration cancellationTokenRegistration;

        public ReceiverLimiter(int maxCount)
        {
            if (maxCount <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            this.maxCount = maxCount;
        }

        public int Count { get; private set; }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (this.locker)
            {
                if (this.taskCompletionSource != null)
                {
                    throw new InvalidOperationException();
                }

                if (this.Count < this.maxCount)
                {
                    return Task.CompletedTask;
                }

                TaskCompletionSource<object?> tcs = new TaskCompletionSource<object?>();
                this.cancellationTokenRegistration = cancellationToken.Register(() => tcs.TrySetCanceled());

                this.taskCompletionSource = tcs;
                return tcs.Task;
            }
        }

        public int OnRecieved(int count)
        {
            lock (this.locker)
            {
                return this.Count += count;
            }
        }

        public int OnClosed(int count)
        {
            TaskCompletionSource<object?>? tcs = null;
            int newCount;
            lock (this.locker)
            {
                newCount = this.Count -= count;
                if (this.Count < this.maxCount)
                {
                    tcs = this.taskCompletionSource;
                    this.taskCompletionSource = null;
                }
            }

            if (tcs != null)
            {
                this.cancellationTokenRegistration.Dispose();
                tcs.TrySetResult(null);
            }

            return newCount;
        }
    }
}
