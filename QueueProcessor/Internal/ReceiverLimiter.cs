using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class ReceiverLimiter
    {
        private readonly object locker = new object();

        private readonly int limit;
        private List<Wait> waits = new List<Wait>();

        public ReceiverLimiter(int limit)
        {
            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "The limit must be at least 1.");
            }

            this.limit = limit;
        }

        public int Count { get; private set; }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            lock (this.locker)
            {
                if (this.Count < this.limit)
                {
                    return Task.CompletedTask;
                }

                Wait wait = new Wait(cancellationToken);
                this.waits.Add(wait);
                return wait.Task;
            }
        }

        public int OnRecieved(int count)
        {
            lock (this.locker)
            {
                return this.Count += count;
            }
        }

        public void OnClosed(int count)
        {
            List<Wait>? waitsToComplete = null;
            lock (this.locker)
            {
                this.Count -= count;
                if (this.Count < this.limit && this.waits.Count > 0)
                {
                    waitsToComplete = this.waits;
                    this.waits = new List<Wait>();
                }
            }

            if (waitsToComplete != null)
            {
                waitsToComplete.ForEach(x => x.Complete());
            }
        }

        private class Wait
        {
            private readonly TaskCompletionSource<object?> taskCompletionSource;
            private readonly CancellationTokenRegistration cancellationTokenRegistration;

            public Wait(CancellationToken cancellationToken)
            {
                this.taskCompletionSource = new TaskCompletionSource<object?>();
                if (cancellationToken.CanBeCanceled)
                {
                    this.cancellationTokenRegistration = cancellationToken.Register(this.Cancel);
                }
            }

            public Task Task => this.taskCompletionSource.Task;

            public void Complete()
            {
                this.cancellationTokenRegistration.Dispose();

                // We use Try here because a race condition can happen between cancelling and completing the task, but we don't really care which wins.
                this.taskCompletionSource.TrySetResult(null);
            }

            private void Cancel()
            {
                this.cancellationTokenRegistration.Dispose();

                // We use Try here because a race condition can happen between cancelling and completing the task, but we don't really care which wins.
                this.taskCompletionSource.TrySetCanceled();
            }
        }
    }
}
