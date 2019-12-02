using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class BackgroundProcess : IDisposable
    {
        private readonly ILogger logger;
        private readonly Func<CancellationToken, Task> mainAsync;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task? task;

        public BackgroundProcess(ILogger logger, Func<CancellationToken, Task> main)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.mainAsync = main ?? throw new ArgumentNullException(nameof(main));
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
                catch (Exception e)
                {
                    if (e is OperationCanceledException oce && oce.CancellationToken == this.cancellationTokenSource.Token)
                    {
                        return;
                    }

                    this.logger.LogError(e);
                }
            }
        }

        public void Dispose() => this.cancellationTokenSource.Dispose();
    }
}
