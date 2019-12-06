using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public delegate Task MessageProcessor<TMessage>(IReadOnlyList<WorkItem<TMessage>> workItems, CancellationToken cancellationToken);

    public sealed class ProcessorService<TMessage> : IProcessorService<TMessage>
    {
        private static readonly Func<WorkItem<TMessage>, Op> OnSuccessDefault = workItem => Op.Close;
        private static readonly Func<WorkItem<TMessage>, Op> OnFailureDefault = workItem => Op.InstantRetry;

        private readonly MessageProcessor<TMessage> processor;
        private readonly ILogger logger;
        private readonly IRetryPolicy retryPolicy;
        private readonly List<BackgroundProcess> backgroundProcesses = new List<BackgroundProcess>();

        public ProcessorService(
            string name,
            MessageProcessor<TMessage> processor,
            ILogger? logger = null,
            int concurrency = 1,
            int maxBatchSize = 0,
            TimeSpan maxBatchDelay = default,
            Func<WorkItem<TMessage>, Op>? onSuccess = null,
            Func<WorkItem<TMessage>, Op>? onFailure = null,
            IRetryPolicy? retryPolicy = null)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Concurrency must be at least 1.");
            }

            this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
            this.logger = logger ?? new DebugLogger();
            this.retryPolicy = retryPolicy ?? new DefaultRetryPolicy(5);
            for (int i = 0; i < concurrency; i++)
            {
                this.backgroundProcesses.Add(new BackgroundProcess(this.logger, this.MainAsync));
            }
        }

        public event Action<IEnumerable<TMessage>> Closed;

        public void Enqueue(IEnumerable<TMessage> messages)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }

        private async Task MainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(this.retryPolicy.GetDelay(), cancellationToken).ConfigureAwait(false);
                List<WorkItem<TMessage>> workItems = new List<WorkItem<TMessage>>();
                try
                {
                    await this.processor(workItems, cancellationToken).ConfigureAwait(false);

                    this.retryPolicy.OnSuccess();
                }
                catch (Exception e)
                {
                    this.retryPolicy.OnFailure();
                }
            }
        }
    }
}
