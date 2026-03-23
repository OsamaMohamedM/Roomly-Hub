using Application.Common.Filters;
using Domain.Entities.Room;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Rooms
                .Include(r => r.Photos)
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        }

        public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
        {
            await _context.Rooms.AddAsync(room, cancellationToken);
        }

        public Task UpdateAsync(Room room, CancellationToken cancellationToken = default)
        {
            _context.Rooms.Update(room);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Room room, CancellationToken cancellationToken = default)
        {
            room.SoftDelete();
            _context.Rooms.Update(room);
            return Task.CompletedTask;
        }

        public async Task<List<Room>> GetByHostIdAsync(Guid hostId, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.HostId == hostId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetByRoomTypeAsync(Domain.enums.Room.RoomType roomType, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.RoomType == roomType)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetByRoomStatusAsync(Domain.enums.Room.RoomAvailabilityStatus roomStatus, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.StatusAvailability == roomStatus)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.PricePerNight >= minPrice && r.PricePerNight <= maxPrice)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetByRatingRangeAsync(decimal minRating, decimal maxRating, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.AverageRating.HasValue && r.AverageRating.Value >= minRating && r.AverageRating.Value <= maxRating)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetByLocationAsync(Address location, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.Address.City == location.City
                            && r.Address.State == location.State
                            && r.Address.Country == location.Country)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Room>, int)> SearchRoomsAsync(RoomFilters query, CancellationToken cancellationToken = default)
        {
            var baseQuery = ApplyFilters(BaseRoomsQuery(), query);

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            var rooms = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (rooms, totalCount);
        }

        public async Task<List<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.StatusAvailability == Domain.enums.Room.RoomAvailabilityStatus.Available)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetRoomsWithFiltersAsync(RoomFilters roomFilters, CancellationToken cancellationToken = default)
        {
            return await ApplyFilters(BaseRoomsQuery(), roomFilters)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Room>> GetPendingReviewRoomsAsync(CancellationToken cancellationToken = default)
        {
            return await BaseRoomsQuery()
                .Where(r => r.Status == Domain.enums.Room.RoomListingStatus.PendingReview)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<Room> BaseRoomsQuery()
        {
            return _context.Rooms
                .Include(r => r.Photos)
                .Include(r => r.Amenities)
                .Where(r => !r.IsDeleted);
        }

        private static IQueryable<Room> ApplyFilters(IQueryable<Room> rooms, RoomFilters filters)
        {
            if (filters.HostId.HasValue)
                rooms = rooms.Where(r => r.HostId == filters.HostId.Value);

            if (filters.RoomType.HasValue)
                rooms = rooms.Where(r => r.RoomType == filters.RoomType.Value);

            if (filters.RoomStatus.HasValue)
                rooms = rooms.Where(r => r.StatusAvailability == filters.RoomStatus.Value);

            if (filters.MinPrice.HasValue)
                rooms = rooms.Where(r => r.PricePerNight >= filters.MinPrice.Value);

            if (filters.MaxPrice.HasValue)
                rooms = rooms.Where(r => r.PricePerNight <= filters.MaxPrice.Value);

            if (filters.MinRating.HasValue)
                rooms = rooms.Where(r => r.AverageRating.HasValue && r.AverageRating.Value >= filters.MinRating.Value);

            if (filters.MaxRating.HasValue)
                rooms = rooms.Where(r => r.AverageRating.HasValue && r.AverageRating.Value <= filters.MaxRating.Value);

            if (filters.Location is not null)
            {
                if (!string.IsNullOrWhiteSpace(filters.Location.City))
                    rooms = rooms.Where(r => r.Address.City == filters.Location.City);

                if (!string.IsNullOrWhiteSpace(filters.Location.State))
                    rooms = rooms.Where(r => r.Address.State == filters.Location.State);

                if (!string.IsNullOrWhiteSpace(filters.Location.Country))
                    rooms = rooms.Where(r => r.Address.Country == filters.Location.Country);
            }

            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var search = filters.SearchQuery.Trim().ToLower();
                rooms = rooms.Where(r =>
                    r.Title.ToLower().Contains(search) ||
                    r.Description.ToLower().Contains(search) ||
                    r.Address.City.ToLower().Contains(search));
            }

            return rooms;
        }
    }
}