namespace Application.DTOs.Payment
{
    public class CardPaymentResponse : BasePaymentResponse
    {
        public string RedirectTo { get; set; }
    }
}