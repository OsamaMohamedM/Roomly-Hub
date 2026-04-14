using Domain.enums.Booking;

namespace Domain.Entities.Payment
{
    public class PaymentWebhookLog : BaseEntity
    {
        public long? InvoiceId { get; set; }

        public string? InvoiceKey { get; set; }

        public string? HashKey { get; set; }

        public string? ReferenceId { get; set; }

        public WebhookType WebhookType { get; set; }

        public string RawPayload { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime ReceivedAt { get; set; }

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public bool IsSuccess { get; set; }

        public string? ProcessingError { get; set; }

        public Guid? BookingId { get; set; }

        public PaymentWebhookLog()
        {
            ReceivedAt = DateTime.UtcNow;
            IsProcessed = false;
            IsSuccess = false;
        }

        public void MarkAsProcessed(bool success, string? error = null)
        {
            IsProcessed = true;
            ProcessedAt = DateTime.UtcNow;
            IsSuccess = success;
            ProcessingError = error;
            MarkUpdated();
        }

        public bool IsIdempotent(long invoiceId, WebhookType webhookType)
        {
            return InvoiceId == invoiceId &&
                   WebhookType == webhookType &&
                   IsProcessed &&
                   IsSuccess &&
                   (DateTime.UtcNow - (ProcessedAt ?? DateTime.UtcNow)).TotalSeconds < 3600;
        }
    }
}