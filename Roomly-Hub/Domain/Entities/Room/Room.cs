using Domain.enums.Room;
using Domain.ValueObjects;

namespace Domain.Entities.Room
{
    public class Room : BaseEntity
    {
        public Room()
        { }

        public string Title { get; set; }
        public decimal AverageRating { get; set; }
        public string Description { get; set; }
        public decimal PricePerNight { get; set; }
        public bool FreeCancellation { get; set; }
        public Address AddressLine { get; set; }
        public RoomType RoomType { get; set; }
        public RoomStatus RoomStatus { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public Guid HostId { get; set; }
        public User Host { get; set; }
        private List<RoomPhotos> _Photos { get; set; }
        private List<RoomAmenities> _Amenities { get; set; }
        private List<RoomReviews> _RoomReviews { get; set; }

        public IReadOnlyCollection<RoomPhotos> Photos { get; set; }
        public IReadOnlyCollection<RoomAmenities> Amenities { get; set; }
        public IReadOnlyCollection<RoomReviews> RoomReviews { get; set; }
    }
}