using System.Text.Json.Serialization;

namespace Domain.Entities.Payment
{
    public class PaymentMethoodModel
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; }

        [JsonPropertyName("name_ar")]
        public string NameAr { get; set; }

        public string Redirect { get; set; }

        public string Logo { get; set; }
    }
}