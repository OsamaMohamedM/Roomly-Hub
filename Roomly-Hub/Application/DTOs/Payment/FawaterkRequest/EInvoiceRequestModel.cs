namespace Application.DTOs.Payment.FawaterkRequest
{
    public class EInvoiceRequestModel
    {
        public int? PaymentMethodId { get; set; }

        public List<CartItemModel> CartItems { get; set; } = new();

        public decimal CartTotal => CartItems?.Sum(item => item.Price * item.Quantity) ?? 0m;

        public string Currency { get; set; } = "EGP";

        public EInvoicePayload? PayLoad { get; set; }

        public EInvoiceRedirectionUrls? RedirectionUrls { get; set; }
    }

    public class EInvoiceResponseModel : BasePaymentResponse
    {
        public EInvoiceResponseData Data { get; set; }
    }
}