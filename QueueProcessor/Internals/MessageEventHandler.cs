using System;

namespace QueueProcessor.Internals
{
    public delegate void MessageEventHandler<TMessage>(ReadOnlySpan<TMessage> messages);
}
