using Domain.Entities.Payment;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PaymentWebhookLogRepository : IPaymentWebhookLogRepository
    {
        private readonly AppDbContext _context;

        public PaymentWebhookLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsDuplicateAsync(long? invoiceId, string? hashKey, string? referenceId, WebhookType webhookType, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<PaymentWebhookLog>()
                .Where(w => w.WebhookType == webhookType && w.IsProcessed && !w.IsDeleted);

            if (invoiceId.HasValue)
            {
                query = query.Where(w => w.InvoiceId == invoiceId.Value);
            }

            if (!string.IsNullOrWhiteSpace(hashKey))
            {
                query = query.Where(w => w.HashKey == hashKey);
            }

            if (!string.IsNullOrWhiteSpace(referenceId))
            {
                query = query.Where(w => w.ReferenceId == referenceId);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task AddAsync(PaymentWebhookLog log, CancellationToken cancellationToken = default)
        {
            await _context.Set<PaymentWebhookLog>().AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(PaymentWebhookLog log, CancellationToken cancellationToken = default)
        {
            _context.Set<PaymentWebhookLog>().Update(log);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PaymentWebhookLog?> GetByInvoiceIdAndTypeAsync(long invoiceId, WebhookType webhookType, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PaymentWebhookLog>()
                .Where(w => w.InvoiceId == invoiceId && w.WebhookType == webhookType && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PaymentWebhookLog?> GetByHashKeyAsync(string hashKey, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PaymentWebhookLog>()
                .Where(w => w.HashKey == hashKey && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PaymentWebhookLog?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PaymentWebhookLog>()
                .Where(w => w.ReferenceId == referenceId && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<PaymentWebhookLog>> GetFailedWebhooksAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<PaymentWebhookLog>()
                .Where(w => w.IsProcessed && !w.IsSuccess && !w.IsDeleted)
                .OrderByDescending(w => w.ProcessedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<PaymentWebhookLog>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<PaymentWebhookLog>()
                .Where(w => w.BookingId == bookingId && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}