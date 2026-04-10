using Domain.enums.Room;
using Domain.ValueObjects;

namespace Application.DTOs
{
    public class CreateRoomRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RoomType RoomType { get; set; }
        public Address AddressLine { get; set; }
        public decimal PricePerNight { get; set; }
        public int MaxGuests { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public bool FreeCancellation { get; set; }
        public List<string> Amenities { get; set; } = [];
    }
}