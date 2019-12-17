﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QueueProcessor.Internal
{
    public sealed class ConcurrentTaskRunner
    {
        private readonly List<TaskRunner> runners = new List<TaskRunner>();

        public ConcurrentTaskRunner(int concurrency, Func<CancellationToken, Task> main, Action<Exception>? onException)
        {
            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrency), concurrency, "Concurrency must be at least 1.");
            }

            for (int i = 0; i < concurrency; i++)
            {
                TaskRunner runner = new TaskRunner(main, onException);
                this.runners.Add(runner);
            }
        }

        public void Start() => this.runners.ForEach(x => x.Start());

        public Task StopAsync() => Task.WhenAll(this.runners.Select(x => x.StopAsync()));
    }
}
