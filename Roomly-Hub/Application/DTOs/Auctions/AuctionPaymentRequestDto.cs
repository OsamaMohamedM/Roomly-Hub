using Domain.enums.Booking;

namespace Application.DTOs.Auctions
{
    public class AuctionPaymentRequestDto
    {
        public Guid AuctionId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
}