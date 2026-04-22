namespace Application.DTOs.Auctions
{
    public class PlaceBidResultDto
    {
        public BidResponseDto Bid { get; set; }
        public bool InsufficientFunds { get; set; }
        public decimal? RequiredAmount { get; set; }
    }
}
