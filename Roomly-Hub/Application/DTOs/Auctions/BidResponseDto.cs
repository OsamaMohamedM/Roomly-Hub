namespace Application.DTOs.Auctions
{
    public class BidResponseDto
    {
        public Guid Id { get; set; }
        public Guid AuctionId { get; set; }
        public decimal Amount { get; set; }
        public bool IsWinning { get; set; }
        public string Status { get; set; }
        public bool InsuranceLocked { get; set; }
        public DateTime PlacedAt { get; set; }
    }
}
