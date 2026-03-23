namespace Application.DTOs.Booking
{
    public class BookingCancelRequestDto
    {
        public BookingCancelRequestDto()
        { }

        public Guid BookingId { get; set; }
        public Guid? GuestId { get; set; }
    }
}