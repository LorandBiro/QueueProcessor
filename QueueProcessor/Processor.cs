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

        private readonly Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> func;
        private readonly ILogger<TMessage> logger;
        private readonly int maxBatchSize;
        private readonly Func<Job<TMessage>, Op> onSuccess;
        private readonly Func<Job<TMessage>, Op> onFailure;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly ConcurrentTaskRunner runner;
        private readonly BatchingQueue<Job<TMessage>> queue;

        public Processor(
            string name,
            Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> func,
            ILogger<TMessage>? logger = null,
            int concurrency = 1,
            int maxBatchSize = 1,
            TimeSpan maxBatchDelay = default,
            Func<Job<TMessage>, Op>? onSuccess = null,
            Func<Job<TMessage>, Op>? onFailure = null,
            ICircuitBreaker? circuitBreaker = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.logger = logger ?? new DebugLogger<TMessage>();
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync, e => this.logger.LogException(this.Name, e));
            this.maxBatchSize = maxBatchSize;
            this.queue = new BatchingQueue<Job<TMessage>>();
            this.onSuccess = onSuccess ?? OnSuccessDefault;
            this.onFailure = onFailure ?? OnFailureDefault;
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(0.5, TimeSpan.FromSeconds(5.0), 10, TimeSpan.FromSeconds(10.0));
        }

        public event Action<IReadOnlyCollection<TMessage>>? Closed;

        public string Name { get; }

        public void Enqueue(IEnumerable<TMessage> messages) => this.queue.Enqueue(messages.Select(x => new Job<TMessage>(x)));

        public void Start() => this.runner.Start();

        public Task StopAsync() => this.runner.StopAsync();

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(this.circuitBreaker.GetDelay() ?? TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                IReadOnlyList<Job<TMessage>> jobs = await this.queue.DequeueAsync(this.maxBatchSize, cancellationToken).ConfigureAwait(false);
                try
                {
                    await this.func(jobs, cancellationToken).ConfigureAwait(false);
                    HandleResults(jobs, this.onSuccess);

                    this.circuitBreaker.OnSuccess();
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
                    this.circuitBreaker.OnFailure();
                }
            }
        }

        private void HandleResults(IReadOnlyList<Job<TMessage>> jobs, Func<Job<TMessage>, Op> onResult)
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
                    this.Closed?.Invoke(jobGroup.Select(x => x.Message).ToList());
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
