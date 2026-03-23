using Domain.enums.Room;
using Domain.ValueObjects;

namespace Domain.Entities.Rooms
{
    public class RoomFilters
    {
        private int _pageSize = 10;
        private const int MaxPageSize = 100;

        public Guid? HostId { get; set; }
        public RoomType? RoomType { get; set; }
        public RoomAvailabilityStatus? RoomStatus { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinRating { get; set; }
        public decimal? MaxRating { get; set; }
        public Address? Location { get; set; }
        public string? SearchQuery { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Page { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 10 : value;
        }
    }
}