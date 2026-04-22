namespace Application.DTOs.Auctions
{
    public class PlaceBidRequestDto
    {
        public Guid AuctionId { get; set; }
        public decimal Amount { get; set; }
    }
}
