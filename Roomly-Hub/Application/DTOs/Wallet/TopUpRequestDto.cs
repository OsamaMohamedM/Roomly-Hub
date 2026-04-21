namespace Application.DTOs.Wallet
{
    public class TopUpRequestDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string? ExternalRef { get; set; }
    }
}