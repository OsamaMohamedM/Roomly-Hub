namespace Application.DTOs.Wallet
{
    public class WalletResponseDto
    {
        public Guid Id { get; set; }
        public decimal Balance { get; set; }
        public decimal InsuranceHeldBalance { get; set; }
    }
}