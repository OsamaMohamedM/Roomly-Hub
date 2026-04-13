using Newtonsoft.Json;


namespace Application.DTOs.Payment
{
    public class EInvoiceResponseData : BasePaymentDataResponse
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}