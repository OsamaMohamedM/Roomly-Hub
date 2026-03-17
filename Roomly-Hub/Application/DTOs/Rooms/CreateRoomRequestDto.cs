namespace Application.DTOs
{
    public class CreateRoomRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string? UnitNumber { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public string BookingMode { get; set; } = string.Empty;
        public bool FreeCancellation { get; set; }
        public List<string> Amenities { get; set; } = [];
    }
}