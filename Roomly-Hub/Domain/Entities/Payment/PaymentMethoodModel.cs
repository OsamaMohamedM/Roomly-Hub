using Newtonsoft.Json;

namespace Domain.Entities.Payment
{
    public class PaymentMethoodModel
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_ar")]
        public string NameAr { get; set; }

        public string Redirect { get; set; }

        public string Logo { get; set; }
    }
}