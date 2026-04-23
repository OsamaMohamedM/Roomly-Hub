namespace Application.DTOs.Auctions
{
    public class CreateAuctionRequestDto
    {
        public Guid RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal StartingPrice { get; set; }
        public string MinBidIncrementType { get; set; }
        public decimal MinBidIncrementValue { get; set; }
        public string Duration { get; set; }
    }
}