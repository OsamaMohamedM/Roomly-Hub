namespace Application.DTOs.Payment
{
    public abstract class BasePaymentDataResponse
    {
        public string InvoiceId { get; set; }

        public string InvoiceKey { get; set; }
    }
}