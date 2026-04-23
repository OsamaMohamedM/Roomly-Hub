namespace Application.DTOs.Auctions
{
    public class AuctionResponseDto
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string RoomTitle { get; set; }
        public Guid HostId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal? CurrentHighestBid { get; set; }
        public decimal InsuranceDepositAmount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int BidCount { get; set; }
        public decimal MinNextBid { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}