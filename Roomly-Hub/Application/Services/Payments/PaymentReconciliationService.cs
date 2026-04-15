using Application.Interfaces.Services;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;

namespace Application.Services.Payments
{
    public class PaymentReconciliationService : IPaymentReconciliationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentWebhookService _webhookService;

        public PaymentReconciliationService(
            IBookingRepository bookingRepository,
            IPaymentService paymentService,
            IPaymentWebhookService webhookService)
        {
            _bookingRepository = bookingRepository;
            _paymentService = paymentService;
            _webhookService = webhookService;
        }

        public async Task ReconcilePendingPaymentsAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-2);
            var pendingBookings = await _bookingRepository.GetUnpaidBookingsOlderThanAsync(cutoffTime);

            foreach (var booking in pendingBookings)
            {
                if (string.IsNullOrWhiteSpace(booking.PaymentInvoiceId))
                    continue;

                var status = await _paymentService.CheckInvoiceStatusAsync(booking.PaymentInvoiceId);
                if (status == PaymentStatus.Paid)
                {
                    await _webhookService.HandleSuccessWebhookAsync(booking.PaymentInvoiceId);
                }
            }
        }
    }
}