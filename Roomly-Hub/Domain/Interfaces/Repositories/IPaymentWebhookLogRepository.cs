using Domain.Entities.Payment;
using Domain.enums.Booking;

namespace Domain.Interfaces.Repositories
{
    public interface IPaymentWebhookLogRepository
    {
        Task AddAsync(PaymentWebhookLog log, CancellationToken cancellationToken = default);

        Task UpdateAsync(PaymentWebhookLog log, CancellationToken cancellationToken = default);

        Task<PaymentWebhookLog?> GetByInvoiceIdAndTypeAsync(long invoiceId, WebhookType webhookType, CancellationToken cancellationToken = default);

        Task<PaymentWebhookLog?> GetByHashKeyAsync(string hashKey, CancellationToken cancellationToken = default);

        Task<PaymentWebhookLog?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

        Task<IEnumerable<PaymentWebhookLog>> GetFailedWebhooksAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<PaymentWebhookLog>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }
}