namespace Application.DTOs
{
    public class RoomResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public string BookingMode { get; set; } = string.Empty;
        public bool FreeCancellation { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? AverageRating { get; set; }
        public List<RoomPhotoDto> Photos { get; set; } = [];
        public List<string> Amenities { get; set; } = [];
    }
}