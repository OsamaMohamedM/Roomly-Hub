using Newtonsoft.Json;

namespace Application.DTOs.Payment.FawaterkRequest
{
    public class EInvoiceRedirectionUrls
    {

        [JsonProperty("successUrl")]
        public string? OnSuccess { get; set; }

        [JsonProperty("failUrl")]
        public string? OnFailure { get; set; }

        [JsonProperty("pendingUrl")]
        public string? OnPending { get; set; }
    }
}