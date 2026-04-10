namespace Application.DTOs.Payment
{
    public class MeezaPaymentResponse : BasePaymentResponse
    {
        public string ReferenceCode { get; set; }

        public DateTime ExpireDate { get; set; }

        public string TransactionStatus { get; set; }
    }
}
