using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class ReceiverLimiter
    {
        private List<Wait> waits = new List<Wait>();

        public bool IsEnabled { get; private set; }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (!this.IsEnabled)
            {
                return Task.CompletedTask;
            }

            Wait wait = new Wait(cancellationToken);
            this.waits.Add(wait);
            return wait.Task;
        }

        public void Enable()
        {
            this.IsEnabled = true;
        }

        public void Disable()
        {
            this.IsEnabled = false;

            List<Wait> waitsToComplete = this.waits;
            this.waits = new List<Wait>();

            waitsToComplete.ForEach(x => x.Complete());
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
