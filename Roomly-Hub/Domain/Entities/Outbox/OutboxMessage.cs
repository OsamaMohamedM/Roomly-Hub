using Domain.Entities;

namespace Domain.Entities.Outbox
{
    public class OutboxMessage : BaseEntity
    {
        private OutboxMessage()
        {
            Type = string.Empty;
            Payload = string.Empty;
        }

        public string Type { get; private set; }
        public string Payload { get; private set; }
        public DateTime OccurredAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public int RetryCount { get; private set; }
        public string? Error { get; private set; }

        public static OutboxMessage Create(string type, string payload)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Outbox message type is required.", nameof(type));

            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("Outbox message payload is required.", nameof(payload));

            return new OutboxMessage
            {
                Type = type,
                Payload = payload,
                OccurredAt = DateTime.UtcNow
            };
        }

        public void MarkProcessed()
        {
            ProcessedAt = DateTime.UtcNow;
            Error = null;
            MarkUpdated();
        }

        public void MarkFailed(string error)
        {
            RetryCount++;
            Error = string.IsNullOrWhiteSpace(error) ? "Unknown outbox processing error." : error;
            MarkUpdated();
        }
    }
}
