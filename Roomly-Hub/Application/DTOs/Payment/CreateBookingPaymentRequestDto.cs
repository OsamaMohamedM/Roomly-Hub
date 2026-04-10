using Application.DTOs.Payment.FawaterkRequest;

namespace Application.DTOs.Payment
{
    public class CreateBookingPaymentRequestDto
    {
        public Guid BookingId { get; set; }

        public int PaymentMethodId { get; set; }

        public EInvoiceRedirectionUrls? RedirectionUrls { get; set; }
    }
}
