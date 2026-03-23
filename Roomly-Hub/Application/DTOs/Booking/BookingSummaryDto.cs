using Domain.Entities.Rooms;
using Domain.enums.Booking;

namespace Application.DTOs.Booking
{
    public class BookingSummaryDto
    {
        public BookingSummaryDto()
        { }

        public Room Room { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}