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
    }
}