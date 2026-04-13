namespace Application.DTOs.Payment.FawaterkRequest
{
    public class CartItemModel
    {
        public decimal PricePerNight { get; set; }

        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public string Currency { get; set; }

        public string Description { get; set; }
        public int Quantity { get; set; }
    }
}