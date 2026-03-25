namespace Application.DTOs
{
    public class AvailabilityResponseDto
    {
        public Guid RoomId { get; set; }
        public List<DateOnly> BlockedDates { get; set; } = [];
    }
}