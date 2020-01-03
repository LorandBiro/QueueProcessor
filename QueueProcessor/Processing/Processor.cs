using QueueProcessor.CircuitBreaking;
using QueueProcessor.Logging;
using QueueProcessor.Timers;
using QueueProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Processing
{
    public sealed class Processor<TMessage> : IProcessor<TMessage>
    {
        private static readonly Func<Job<TMessage>, IProcessor<TMessage>?> DefaultRouter = job => null;

        private readonly Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> func;
        private readonly ILogger<TMessage> logger;
        private readonly Func<Job<TMessage>, IProcessor<TMessage>?> onSuccess;
        private readonly Func<Job<TMessage>, IProcessor<TMessage>?> onFailure;
        private readonly ICircuitBreaker circuitBreaker;
        private readonly ConcurrentTaskRunner runner;
        private readonly BatchingQueue<TMessage> queue;
        private readonly Predicate<(TMessage Message, string? ErrorCode, Exception? Exception)> retry;

        public Processor(
            string name,
            Func<IReadOnlyList<Job<TMessage>>, CancellationToken, Task> func,
            ILogger<TMessage>? logger = null,
            int concurrency = 1,
            int maxBatchSize = 1,
            TimeSpan maxBatchDelay = default,
            Func<Job<TMessage>, IProcessor<TMessage>?>? onSuccess = null,
            Func<Job<TMessage>, IProcessor<TMessage>?>? onFailure = null,
            ICircuitBreaker? circuitBreaker = null,
            Predicate<(TMessage Message, string? ErrorCode, Exception? Exception)>? retry = null)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.func = func ?? throw new ArgumentNullException(nameof(func));
            this.logger = logger ?? NullLogger<TMessage>.Instance;
            this.runner = new ConcurrentTaskRunner(concurrency, this.MainAsync, e => this.logger.LogException(this.Name, e));
            this.queue = new BatchingQueue<TMessage>(Clock.Instance, maxBatchSize, maxBatchDelay);
            this.onSuccess = onSuccess ?? DefaultRouter;
            this.onFailure = onFailure ?? DefaultRouter;
            this.circuitBreaker = circuitBreaker ?? new CircuitBreaker(0.5, new IntervalTimer(TimeSpan.FromSeconds(5.0)), 10, TimeSpan.FromSeconds(10.0));
            this.retry = retry ?? (_ => false);
        }

        public event Action<IReadOnlyCollection<TMessage>>? Closed;

        public string Name { get; }

        public void Enqueue(IEnumerable<TMessage> messages) => this.queue.Enqueue(messages);

        public void Start()
        {
            this.runner.Start();
            this.queue.Start();
        }

        public async ValueTask DisposeAsync() => await Task.WhenAll(this.runner.DisposeAsync().AsTask(), this.queue.DisposeAsync().AsTask()).ConfigureAwait(false);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't know what exceptions to expect here, so we need to catch all.")]
        private async Task MainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Delay(this.circuitBreaker.GetDelay() ?? TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                IReadOnlyList<TMessage> messages = await this.queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                List<Job<TMessage>> jobs = messages.Select(x => new Job<TMessage>(x)).ToList();
                try
                {
                    await this.func(jobs, cancellationToken).ConfigureAwait(false);
                    HandleResults(jobs);

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

                    this.PerformRetryLogic(jobs);

                    this.HandleResults(jobs);
                    this.circuitBreaker.OnFailure();
                }
            }
        }

        private void PerformRetryLogic(List<Job<TMessage>> jobs)
        {
            List<TMessage> messagesToRetry = new List<TMessage>();
            for (int i = 0; i < jobs.Count;)
            {
                if (jobs[i].Result.IsError && this.retry((jobs[i].Message, jobs[i].Result.Code, jobs[i].Result.Exception)))
                {
                    messagesToRetry.Add(jobs[i].Message);
                    jobs.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            if (messagesToRetry.Count > 0)
            {
                this.queue.Enqueue(messagesToRetry);
            }
        }

        private void HandleResults(IReadOnlyList<Job<TMessage>> jobs)
        {
            List<(Job<TMessage> Job, IProcessor<TMessage>? Route)> jobRouteMap = jobs.Select(x => (Job: x, Route: (x.Result.IsError ? this.onFailure : this.onSuccess)(x))).ToList();
            foreach ((Job<TMessage> Job, IProcessor<TMessage>? Route) jobRoutePair in jobRouteMap)
            {
                this.logger.LogMessageProcessed(this.Name, jobRoutePair.Job.Message, jobRoutePair.Job.Result, jobRoutePair.Route);
            }

            foreach (IGrouping<IProcessor<TMessage>?, TMessage> group in jobRouteMap.GroupBy(x => x.Route, x => x.Job.Message))
            {
                if (group.Key == null)
                {
                    this.Closed?.Invoke(group.ToList());
                    continue;
                }

                group.Key.Enqueue(group);
            }
        }
    }
}
