using Newtonsoft.Json;

namespace Application.DTOs.Payment
{
    public abstract class BasePaymentDataResponse
    {
        [JsonProperty("invoiceKey")]
        public string InvoiceKey { get; set; }

        [JsonProperty("invoiceId")]
        public string InvoiceId { get; set; }
    }
}