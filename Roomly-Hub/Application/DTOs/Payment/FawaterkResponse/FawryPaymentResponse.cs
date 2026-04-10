namespace Application.DTOs.Payment
{
    public class FawryPaymentResponse : BasePaymentResponse
    {
        public string FawryCode { get; set; }

        public DateTime ExpireDate { get; set; }
    }
}