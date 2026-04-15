using Newtonsoft.Json;

namespace Application.DTOs.Payment.FawaterkResponse
{
    public class InvoiceStatusResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("data")]
        public InvoiceStatusData? Data { get; set; }
    }

    public class InvoiceStatusData
    {
        [JsonProperty("invoice_id")]
        public long InvoiceId { get; set; }

        [JsonProperty("invoice_key")]
        public string InvoiceKey { get; set; } = string.Empty;

        [JsonProperty("paid")]
        public int Paid { get; set; }

        [JsonProperty("paid_at")]
        public DateTime? PaidAt { get; set; }
    }
}