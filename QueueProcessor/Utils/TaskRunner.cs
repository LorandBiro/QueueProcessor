using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Utils
{
    public sealed class TaskRunner : IAsyncDisposable
    {
        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(1.0);

        private readonly Func<CancellationToken, Task> mainAsync;
        private readonly Action<Exception>? onException;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task? task;
        private bool isDisposed;

        public TaskRunner(Func<CancellationToken, Task> main, Action<Exception>? onException)
        {
            this.mainAsync = main ?? throw new ArgumentNullException(nameof(main));
            this.onException = onException;
        }

        public void Start()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(null);
            }

            if (this.task != null)
            {
                throw new InvalidOperationException();
            }

            this.task = Task.Run(this.MainAsync);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We have to catch everything, we can't let background processes crash.")]
        private async Task MainAsync()
        {
            while (true)
            {
                try
                {
                    await this.mainAsync(this.cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (exception is OperationCanceledException oce && oce.CancellationToken == this.cancellationTokenSource.Token)
                    {
                        return;
                    }

                    this.onException?.Invoke(exception);
                    await Task.Delay(RestartDelay).ConfigureAwait(false);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.task != null)
            {
                this.cancellationTokenSource.Cancel();
                await this.task.ConfigureAwait(false);
            }

            this.cancellationTokenSource.Dispose();
            this.isDisposed = true;
        }
    }
}
