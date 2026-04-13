using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Application.DTOs.Payment.FawaterkRequest
{
    public class EInvoiceRequestModel
    {
        [JsonProperty("payment_method_id")]
        public int? PaymentMethodId { get; set; }

        [JsonProperty("cartTotal")]
        public string CartTotal { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("customer")]
        public CustomerModel Customer { get; set; }

        [JsonProperty("cartItems")]
        public List<CartItemModel> CartItems { get; set; }

        [JsonProperty("redirectionUrls")]
        public EInvoiceRedirectionUrls? RedirectionUrls { get; set; }
    }

    public class CustomerModel
    {
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }

    public class CartItemModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }
    }

    public class EInvoiceResponseModel : BasePaymentResponse
    {
        public EInvoiceResponseData Data { get; set; }
    }
}