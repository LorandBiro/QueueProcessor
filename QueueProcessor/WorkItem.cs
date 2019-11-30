using QueueProcessor.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueueProcessor
{
    public sealed class WorkItem<TMessage>
    {
        public WorkItem(TMessage message)
        {
            this.Message = message;
        }

        public TMessage Message { get; }

        public int ErrorCount { get; private set; }

        public void Next(IProcessorService<TMessage> processor, Result result = default) { }
        public void Close(Result result = default) { }
        public void Retry(TimeSpan delay = default, Result result = default) { }
    }
}
