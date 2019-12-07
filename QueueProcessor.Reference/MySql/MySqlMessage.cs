using System;

namespace QueueProcessor.MySql
{
    public sealed class MySqlMessage
    {
        public MySqlMessage(long id, string payload, DateTime enqueuedAt, DateTime visibilityTimeout, int receivedCount)
        {
            this.Id = id;
            this.Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            this.EnqueuedTimestamp = enqueuedAt;
            this.Timeout = visibilityTimeout;
            this.ReceivedCount = receivedCount;
        }

        public long Id { get; }

        public string Payload { get; }

        public DateTime EnqueuedTimestamp { get; }

        public DateTime Timeout { get; }

        public int ReceivedCount { get; }

        public override string ToString() => this.Payload;
    }
}
