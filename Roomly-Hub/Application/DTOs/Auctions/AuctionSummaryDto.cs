namespace Application.DTOs.Auctions
{
    public class AuctionSummaryDto
    {
        public Guid Id { get; set; }
        public string RoomTitle { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal? CurrentHighestBid { get; set; }
        public decimal InsuranceDepositAmount { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public int BidCount { get; set; }
    }
}
