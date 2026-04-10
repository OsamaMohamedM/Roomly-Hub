using Domain.Entities.Payment;

namespace Application.DTOs.Payment
{
    public class PaymentMethodsResponse
    {
        public string Status { get; set; }
        public List<PaymentMethoodModel> Data { get; set; }
    }
}