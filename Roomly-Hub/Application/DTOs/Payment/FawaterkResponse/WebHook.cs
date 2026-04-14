using Domain.enums.Booking;
using Newtonsoft.Json;

namespace Application.DTOs.Payment
{
    public class WebHookModel
    {
        [JsonProperty("invoice_id")]
        public long InvoiceId { get; set; }

        [JsonProperty("invoice_key")]
        public string InvoiceKey { get; set; }

        [JsonProperty("hashKey")]
        public string HashKey { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("invoice_status")]
        public string InvoiceStatus { get; set; }

        [JsonProperty("paidAmount")]
        public decimal PaidAmount { get; set; }

        [JsonProperty("paidCurrency")]
        public string PaidCurrency { get; set; }

        [JsonProperty("paidAt")]
        public DateTime paidAt { get; set; }
    }

    public class CancelTransactionModel
    {
        [JsonProperty("hashKey")]
        public string HashKey { get; set; }

        [JsonProperty("referenceId")]
        public string ReferenceId { get; set; }

        public string Status { get; set; }

        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }
    }

    public class FaliledWebHook
    {
        [JsonProperty("invoice_id")]
        public long InvoiceId { get; set; }

        [JsonProperty("invoice_key")]
        public string InvoiceKey { get; set; }

        public string ErrorMessage { get; set; }
    }
}