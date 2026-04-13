namespace Application.DTOs
{
    public class UpdateRoomRequestDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? PricePerNight { get; set; }
        public int? MaxGuests { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string? BookingMode { get; set; }
        public bool? FreeCancellation { get; set; }
        public List<Guid>? AmenityIds { get; set; }
    }
}
