using Domain.enums.Booking;

namespace Application.DTOs.Payment
{
    public class BookingPaymentStatusDto
    {
        public Guid BookingId { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}
