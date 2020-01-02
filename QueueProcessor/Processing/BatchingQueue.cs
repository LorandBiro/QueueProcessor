using QueueProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Processing
{
    /// <summary>
    /// An asynchronous producer-consumer queue with batching capability. Items can be enqueued one-by-one but a consumer will be served with a batch of items
    /// only when the number of enqueued items reach a maximum batch size or when an enqueued item was in the queue for longer than the minimum duration.
    /// </summary>
    /// <remarks>
    /// This producer-consumer queue contains a queue of closed batches and an open batch where the enqueued items are added. A batch will be closed when one
    /// of the following conditions are met:
    /// - The size of the open batch reaches the configured maximum size.
    /// - The age of the batch reaches the configured minimum and there is at least one waiting consumer.
    /// Closed batches will be enqeueued to the closed batch queue. When a consumer tries to dequeue a batch, it will be either served from the closed batch
    /// queue or put into a consumer queue so it can be served in a FIFO order.
    /// </remarks>
    /// <typeparam name="T">The type of the items in the queue.</typeparam>
    public sealed class BatchingQueue<T>
    {
        // Dependencies
        private readonly IClock clock;

        // Configuration
        private readonly int maxBatchSize;
        private readonly TimeSpan minBatchDuration;

        // State
        private readonly TaskRunner runner;
        private readonly object locker = new object();
        private readonly Queue<Consumer> consumerQueue = new Queue<Consumer>();
        private readonly Queue<Batch> closedBatches = new Queue<Batch>();
        private volatile Batch? openBatch; // Volatile is for double-checked locking.

        public BatchingQueue(IClock time, int maxBatchSize, TimeSpan minBatchDuration)
        {
            if (maxBatchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBatchSize), maxBatchSize, "Maximum batch size must be larger than 0.");
            }

            if (minBatchDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minBatchDuration), minBatchDuration, "Minimum batch age must be larger than 0.");
            }

            this.clock = time ?? throw new ArgumentNullException(nameof(time));
            this.maxBatchSize = maxBatchSize;
            this.minBatchDuration = minBatchDuration;
            this.runner = new TaskRunner(this.TimerMainAsync, null);
        }

        public int Count { get; private set; }

        public void Start() => this.runner.Start();

        public Task StopAsync() => this.runner.StopAsync();

        public void Enqueue(T item)
        {
            lock (this.locker)
            {
                this.EnqueueInternal(item);
            }
        }

        public void Enqueue(IEnumerable<T> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            lock (this.locker)
            {
                foreach (T item in items)
                {
                    this.EnqueueInternal(item);
                }
            }
        }

        private void EnqueueInternal(T item)
        {
            this.Count++;
            if (this.openBatch == null)
            {
                this.openBatch = new Batch(this.clock.Now, item);
            }
            else
            {
                this.openBatch.Items.Add(item);
            }

            if (this.openBatch.Items.Count >= this.maxBatchSize)
            {
                do
                {
                    if (this.consumerQueue.Count > 0)
                    {
                        bool success = this.consumerQueue.Dequeue().TrySetResult(this.openBatch.Items);
                        if (success)
                        {
                            this.Count -= this.openBatch.Items.Count;
                            this.openBatch = null;
                        }
                    }
                    else
                    {
                        this.closedBatches.Enqueue(this.openBatch);
                        this.openBatch = null;
                    }
                } while (this.openBatch != null);
            }
        }

        public Task<IReadOnlyList<T>> DequeueAsync(CancellationToken cancellationToken = default)
        {
            lock (this.locker)
            {
                if (this.closedBatches.Count > 0)
                {
                    Batch batch = this.closedBatches.Dequeue();
                    return Task.FromResult<IReadOnlyList<T>>(batch.Items);
                }

                Consumer consumer = new Consumer(cancellationToken);
                this.consumerQueue.Enqueue(consumer);
                return consumer.Task;
            }
        }

        private async Task TimerMainAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                TimeSpan duration = this.TimerTickAndGetDelay();
                await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
            }
        }

        private TimeSpan TimerTickAndGetDelay()
        {
            if (this.openBatch == null)
            {
                // Double-checked locking.
                return this.minBatchDuration;
            }

            lock (this.locker)
            {
                if (this.openBatch == null)
                {
                    return this.minBatchDuration;
                }

                TimeSpan timeUntilCurrentBatchClose = this.openBatch.CreatedAt + this.minBatchDuration - this.clock.Now;
                if (timeUntilCurrentBatchClose > TimeSpan.Zero)
                {
                    // This batch still has some time before we have to close it.
                    return timeUntilCurrentBatchClose;
                }

                // This batch should be closed because it's timed out. 
                while (true)
                {
                    if (this.consumerQueue.Count > 0)
                    {
                        // Someone is waiting for a batch to close, let's try serve them.
                        bool success = this.consumerQueue.Dequeue().TrySetResult(this.openBatch.Items);
                        if (success)
                        {
                            this.Count -= this.openBatch.Items.Count;
                            this.openBatch = null;
                            return this.minBatchDuration;
                        }
                    }
                    else
                    {
                        // Nobody waits for a batch right now, so we don't need to time out the current batch. We will time out when it gets too big or
                        // when someone tries to dequeue a batch from the queue. Until then there's no point in closing this batch.
                        return this.minBatchDuration;
                    }
                }
            }
        }

        private class Batch
        {
            public Batch(DateTime createdAt, T item)
            {
                this.CreatedAt = createdAt;
                this.Items = new List<T> { item };
            }

            public DateTime CreatedAt { get; }

            public List<T> Items { get; }
        }

        private sealed class Consumer
        {
            private readonly TaskCompletionSource<IReadOnlyList<T>> taskCompletionSource;
            private readonly CancellationTokenRegistration cancellationTokenRegistration;

            public Consumer(CancellationToken cancellationToken)
            {
                // RunContinuationsAsynchronously is important because otherwise the containuation would run on the current thread. E.g. preparing the SQS
                // call would run as part of the poll operation. The performance impact of this option is significant.
                this.taskCompletionSource = new TaskCompletionSource<IReadOnlyList<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.cancellationTokenRegistration =
                    cancellationToken.Register(() => this.taskCompletionSource.TrySetCanceled(cancellationToken));
            }

            public Task<IReadOnlyList<T>> Task => this.taskCompletionSource.Task;

            public bool TrySetResult(IReadOnlyList<T> items)
            {
                bool result = this.taskCompletionSource.TrySetResult(items);
                this.cancellationTokenRegistration.Dispose();
                return result;
            }
        }
    }
}
