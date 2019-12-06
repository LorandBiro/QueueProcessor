using QueueProcessor.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor
{
    public sealed class Processor<TMessage> : IProcessor<TMessage>
    {
        private static readonly Func<Job<TMessage>, Op> OnSuccessDefault = job => Op.Close;
        private static readonly Func<Job<TMessage>, Op> OnFailureDefault = job => Op.InstantRetry;

        private readonly Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> processor;
        private readonly ILogger<TMessage> logger;
        private readonly Func<Job<TMessage>, Op> onSuccess;
        private readonly Func<Job<TMessage>, Op> onFailure;
        private readonly IRetryPolicy retryPolicy;
        private readonly List<TaskRunner> backgroundProcesses = new List<TaskRunner>();

        public Processor(
            string name,
            Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> processor,
            ILogger<TMessage>? logger = null,
            int concurrency = 1,
            int maxBatchSize = 0,
            TimeSpan maxBatchDelay = default,
            Func<Job<TMessage>, Op>? onSuccess = null,
            Func<Job<TMessage>, Op>? onFailure = null,
            IRetryPolicy? retryPolicy = null)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Concurrency must be at least 1.");
            }

            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.onSuccess = onSuccess ?? OnSuccessDefault;
            this.onFailure = onFailure ?? OnFailureDefault;
            this.retryPolicy = retryPolicy ?? new DefaultRetryPolicy(5);
            for (int i = 0; i < concurrency; i++)
            {
                TaskRunner runner = new TaskRunner(this.MainAsync);
                runner.Exception += (sender, e) => this.logger.LogServiceException(this.Name, e.Exception);
                this.backgroundProcesses.Add(runner);
            }
        }

        public string Name { get; }

        public event Action<IEnumerable<TMessage>>? Closed;

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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(this.retryPolicy.GetDelay(), cancellationToken).ConfigureAwait(false);
                List<Job<TMessage>> jobs = new List<Job<TMessage>>();
                try
                {
                    await this.processor(jobs, cancellationToken).ConfigureAwait(false);
                    HandleResults(jobs, this.onSuccess);

                    this.retryPolicy.OnSuccess();
                }
                catch (Exception exception)
                {
                    if (exception is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                    {
                        throw;
                    }

                    foreach (Job<TMessage> job in jobs)
                    {
                        job.SetResult(Result.Error(exception));
                    }

                    HandleResults(jobs, this.onFailure);
                    this.retryPolicy.OnFailure();
                }
            }
        }

        private void HandleResults(List<Job<TMessage>> jobs, Func<Job<TMessage>, Op> onResult)
        {
            var jobsWithOps = jobs.Select(x => new { x.Message, x.Result, Op = onResult(x) }).ToList();
            foreach (var jobWithOp in jobsWithOps)
            {
                this.logger.LogMessageProcessed(this.Name, jobWithOp.Message, jobWithOp.Result, jobWithOp.Op);
            }

            foreach (var jobGroup in jobsWithOps.GroupBy(x => x.Op.GetType()))
            {
                if (jobGroup.Key == typeof(CloseOp))
                {
                    this.Closed?.Invoke(jobGroup.Select(x => x.Message));
                }
                else if (jobGroup.Key == typeof(RetryOp))
                {
                    foreach (RetryOp retry in jobGroup.Cast<RetryOp>())
                    {
                        // Insert into queue
                    }
                }
                else if (jobGroup.Key == typeof(TransferOp<TMessage>))
                {
                    foreach (var transferGroup in jobGroup.Select(x => new { x.Message, Op = (TransferOp < TMessage > )x.Op }).GroupBy(x => x.Op.Processor, x => x.Message))
                    {
                        transferGroup.Key.Enqueue(transferGroup);
                    }
                }
            }
        }
    }
}
