using Domain.enums.Booking;

namespace Application.DTOs.Payment
{
    public class BookingPaymentLinkResponseDto
    {
        public Guid BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public string PaymentUrl { get; set; }
    }
}