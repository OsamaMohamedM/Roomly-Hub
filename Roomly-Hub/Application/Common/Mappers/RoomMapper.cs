using Application.DTOs;
using Application.DTOs.Rooms;
using Domain.Entities.Rooms;

namespace Application.Common.Mappers
{
    public interface IRoomMapper
    {
        RoomResponseDto ToResponseDto(Room room);

        RoomSummaryDto ToSummaryDto(Room room);

        PendingRoomDto ToPendingRoomDto(Room room);
    }

    public class RoomMapper : IRoomMapper
    {
        public RoomResponseDto ToResponseDto(Room room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            return new RoomResponseDto
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description,
                RoomType = room.RoomType.ToString(),
                Address = FormatAddress(room.Address),
                City = room.Address.City,
                PricePerNight = room.PricePerNight,
                MaxGuests = room.MaxGuests,
                FreeCancellation = room.FreeCancellation,
                Status = room.Status.ToString(),
                AverageRating = room.AverageRating,
                Photos = room.Photos.Select(p => new RoomPhotoDto { Id = p.Id, Url = p.Url }).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList()
            };
        }

        public RoomSummaryDto ToSummaryDto(Room room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            return new RoomSummaryDto
            {
                Id = room.Id,
                Title = room.Title,
                City = room.Address.City,
                PricePerNight = room.PricePerNight,
                AverageRating = room.AverageRating,
                ThumbnailUrl = room.Photos.FirstOrDefault()?.Url,
                FreeCancellation = room.FreeCancellation
            };
        }

        public PendingRoomDto ToPendingRoomDto(Room room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));

            return new PendingRoomDto
            {
                Id = room.Id,
                HostId = room.HostId,
                SubmittedAt = room.CreatedAt,
                Room = room
            };
        }

        private static string FormatAddress(Domain.ValueObjects.Address address)
        {
            return $"{address.Street}, {address.City}, {address.State}, {address.ZipCode}";
        }
    }
}