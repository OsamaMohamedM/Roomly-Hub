using Domain.enums.Booking;

namespace Application.DTOs.Booking
{
    public class HostBookingRequestDto
    {
        public Guid BookingId { get; set; }
        public Guid RoomId { get; set; }
        public Guid GuestId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
