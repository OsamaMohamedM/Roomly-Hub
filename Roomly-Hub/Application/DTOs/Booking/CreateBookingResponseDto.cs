using Domain.enums.Booking;

namespace Application.DTOs.Booking
{
    public class CreateBookingResponseDto
    {
        public Guid BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public string? PaymentUrl { get; set; }
    }
}