using Domain.Entities.Payment;
using System.Text.Json.Serialization;

namespace Application.DTOs.Payment
{
    public class PaymentMethodsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("data")]
        public List<PaymentMethoodModel> Data { get; set; }
    }
}