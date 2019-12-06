using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class BatchingQueue<T>
    {
        private readonly Queue<T> itemQueue = new Queue<T>();
        private readonly Queue<Consumer> consumerQueue = new Queue<Consumer>();

        public void Enqueue(IEnumerable<T> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            lock (this.itemQueue)
            {
                foreach (T item in items)
                {
                    this.itemQueue.Enqueue(item);
                }

                while (this.itemQueue.Count > 0 && this.consumerQueue.Count > 0)
                {
                    Consumer consumer = this.consumerQueue.Dequeue();

                    int batchSize = Math.Min(consumer.BatchSize, this.itemQueue.Count);
                    List<T> batch = new List<T>();
                    for (int i = 0; i < batchSize; i++)
                    {
                        batch.Add(this.itemQueue.Dequeue());
                    }

                    if (!consumer.TrySetResult(batch))
                    {
                        foreach (T item in batch)
                        {
                            this.itemQueue.Enqueue(item);
                        }
                    }
                }
            }
        }

        public Task<IReadOnlyList<T>> DequeueAsync(int batchSize, CancellationToken cancellationToken)
        {
            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize));
            }

            lock (this.itemQueue)
            {
                if (this.itemQueue.Count == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Consumer consumer = new Consumer(batchSize, cancellationToken);
                    this.consumerQueue.Enqueue(consumer);
                    return consumer.Task;
                }

                batchSize = Math.Min(batchSize, this.itemQueue.Count);
                List<T> batch = new List<T>();
                for (int i = 0; i < batchSize; i++)
                {
                    batch.Add(this.itemQueue.Dequeue());
                }

                return Task.FromResult<IReadOnlyList<T>>(batch);
            }
        }

        private sealed class Consumer
        {
            private readonly TaskCompletionSource<IReadOnlyList<T>> taskCompletionSource;
            private readonly CancellationTokenRegistration cancellationTokenRegistration;

            public Consumer(int batchSize, CancellationToken cancellationToken)
            {
                this.BatchSize = batchSize;

                // RunContinuationsAsynchronously is important because otherwise the containuation would run on the current thread. E.g. preparing the SQS
                // call would run as part of the poll operation. The performance impact of this option is significant.
                this.taskCompletionSource = new TaskCompletionSource<IReadOnlyList<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.cancellationTokenRegistration =
                    cancellationToken.Register(() => this.taskCompletionSource.TrySetCanceled(cancellationToken));
            }

            public int BatchSize { get; }

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
