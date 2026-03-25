using Domain.Entities.Rooms;
using Domain.enums.Room;
using Domain.ValueObjects;

namespace Domain.Interfaces.Repositories
{
    public interface IRoomRepository
    {
        public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        public Task AddAsync(Room room, CancellationToken cancellationToken = default);

        public Task UpdateAsync(Room room, CancellationToken cancellationToken = default);

        public Task DeleteAsync(Room room, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByHostIdAsync(Guid hostId, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByRoomTypeAsync(RoomType roomType, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByRoomStatusAsync(RoomAvailabilityStatus roomStatus, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByRatingRangeAsync(decimal minRating, decimal maxRating, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetByLocationAsync(Address location, CancellationToken cancellationToken = default);

        public Task<(List<Room>, int)> SearchRoomsAsync(RoomFilters query, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetRoomsWithFiltersAsync(RoomFilters roomFilters, CancellationToken cancellationToken = default);

        public Task<List<Room>> GetPendingReviewRoomsAsync(CancellationToken cancellationToken = default);

    
        Task<List<RoomAvailability>> GetBlockedDatesAsync(Guid roomId, int year, int month, CancellationToken cancellationToken = default);
    }
}