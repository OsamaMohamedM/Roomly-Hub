using Application.DTOs.Payment.FawaterkRequest;

namespace Application.DTOs.Payment
{
    public class InitiatePaymentDto
    {
        public int PaymentMethodId { get; set; }
        public EInvoiceRedirectionUrls? RedirectionUrls { get; set; }
    }
}
