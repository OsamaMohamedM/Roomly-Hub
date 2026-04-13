using Domain.enums.Booking;
using Application.DTOs.Payment.FawaterkRequest;

namespace Application.DTOs.Booking
{
    public class BookingRequestDto
    {
        public BookingRequestDto()
        { }

        public Guid? BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public int? PaymentMethodId { get; set; }
        public EInvoiceRedirectionUrls? RedirectionUrls { get; set; }
    }
}