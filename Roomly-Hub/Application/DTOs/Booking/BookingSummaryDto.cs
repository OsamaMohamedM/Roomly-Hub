using Domain.Entities.Rooms;
using Domain.enums.Booking;

namespace Application.DTOs.Booking
{
    public class BookingSummaryDto
    {
        public BookingSummaryDto()
        { }

        public Guid BookingId { get; set; }
        public Guid GuestId { get; set; }
        public Room Room { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus Status { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}