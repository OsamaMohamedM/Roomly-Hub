namespace Application.DTOs
{
    public class RoomSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public decimal? AverageRating { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string BookingMode { get; set; } = string.Empty;
        public bool FreeCancellation { get; set; }
    }
}
