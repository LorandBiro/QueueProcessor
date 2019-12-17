using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class TaskRunner : IDisposable
    {
        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(1.0);

        private readonly Func<CancellationToken, Task> mainAsync;
        private readonly Action<Exception>? onException;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task? task;

        public TaskRunner(Func<CancellationToken, Task> main, Action<Exception>? onException)
        {
            this.mainAsync = main ?? throw new ArgumentNullException(nameof(main));
            this.onException = onException;
        }

        public void Start()
        {
            if (this.task != null)
            {
                throw new InvalidOperationException();
            }

            this.task = Task.Run(this.MainAsync);
        }

        public async Task StopAsync()
        {
            if (this.task == null)
            {
                throw new InvalidOperationException();
            }

            this.cancellationTokenSource.Cancel();
            await this.task.ConfigureAwait(false);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We have to catch everything, we can't let background processes crash.")]
        private async Task MainAsync()
        {
            while (true)
            {
                try
                {
                    await this.mainAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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

        public void Dispose() => this.cancellationTokenSource.Dispose();
    }
}
