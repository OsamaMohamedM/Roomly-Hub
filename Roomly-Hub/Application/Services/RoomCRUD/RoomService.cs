using Application.Common.Filters;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Room;
using Domain.Interfaces.Repositories;

namespace Application.Services.RoomCRUD
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(IRoomRepository roomRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RoomResponseDto>> CreateRoomAsync(Guid hostId, CreateRoomRequestDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(hostId, cancellationToken);
            if (user == null)
                return Result<RoomResponseDto>.Failure("UserNotFound", "The specified user was not found.");

            if (!user.CanCreateListing())
                return Result<RoomResponseDto>.Failure("PermissionDenied", "User does not have permission to create a listing.");

            var room = Room.Create(
                hostId,
                dto.Title,
                dto.Description,
                dto.RoomType,
                dto.AddressLine,
                dto.PricePerNight,
                dto.MaxGuests,
                dto.CheckInTime,
                dto.CheckOutTime,
                dto.FreeCancellation,
                dto.Amenities
            );

            await _roomRepository.AddAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var responseDto = new RoomResponseDto
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description,
                RoomType = room.RoomType.ToString(),
                Address = $"{room.Address.Street}, {room.Address.City}, {room.Address.State}, {room.Address.ZipCode}",
                City = room.Address.City,
                PricePerNight = room.PricePerNight,
                MaxGuests = room.MaxGuests,
                FreeCancellation = room.FreeCancellation,
                Status = room.Status.ToString(),
                AverageRating = room.AverageRating,
                Photos = room.Photos.Select(p => new RoomPhotoDto { Id = p.Id, Url = p.Url }).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList()
            };

            return Result<RoomResponseDto>.Success(responseDto);
        }

        public async Task<Result> DeactivateRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = _roomRepository.GetByIdAsync(roomId, cancellationToken).Result;
            if (room == null)
                return (Result.Failure("RoomNotFound", "The specified room was not found."));
            if (room.HostId != hostId)
                return (Result.Failure("PermissionDenied", "User does not have permission to deactivate this listing."));
            if (!room.CanBeDeactivated())
                return (Result.Failure("InvalidState", "Room cannot be deactivated in its current state."));
            room.Deactivate();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<RoomResponseDto>> GetRoomByIdAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result<RoomResponseDto>.Failure("RoomNotFound", "The specified room was not found.");
            return Result<RoomResponseDto>.Success(new RoomResponseDto
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description,
                RoomType = room.RoomType.ToString(),
                Address = $"{room.Address.Street}, {room.Address.City}, {room.Address.State}, {room.Address.ZipCode}",
                City = room.Address.City,
                PricePerNight = room.PricePerNight,
                MaxGuests = room.MaxGuests,
                FreeCancellation = room.FreeCancellation,
                Status = room.Status.ToString(),
                AverageRating = room.AverageRating,
                Photos = room.Photos.Select(p => new RoomPhotoDto { Id = p.Id, Url = p.Url }).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList()
            });
        }

        public async Task<Result<PagedResult<RoomSummaryDto>>> SearchRoomsAsync(RoomFilters filters)
        {
            var (rooms, totalCount) = await _roomRepository.SearchRoomsAsync(filters);

            var dtos = rooms.Select(r => new RoomSummaryDto
            {
                Id = r.Id,
                Title = r.Title,
                City = r.Address.City,
                PricePerNight = r.PricePerNight,
                AverageRating = r.AverageRating,
                ThumbnailUrl = r.Photos.FirstOrDefault()?.Url,
                FreeCancellation = r.FreeCancellation
            }).ToList();

            var page = filters.Page <= 0 ? 1 : filters.Page;
            var pageSize = filters.PageSize <= 0 ? 10 : filters.PageSize;

            var pagedResult = new PagedResult<RoomSummaryDto>(
                dtos,
                totalCount,
                page,
                pageSize);

            return Result<PagedResult<RoomSummaryDto>>.Success(pagedResult);
        }

        public async Task<Result> SubmitForReviewAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result.Failure("RoomNotFound", "The specified room was not found.");
            if (room.HostId != hostId)
                return Result.Failure("PermissionDenied", "User does not have permission to submit this listing for review.");
            room.SubmitForReview();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<RoomResponseDto>> UpdateRoomAsync(Guid hostId, Guid roomId, UpdateRoomRequestDto dto, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result<RoomResponseDto>.Failure("RoomNotFound", "The specified room was not found.");
            if (room.HostId != hostId)
                return Result<RoomResponseDto>.Failure("PermissionDenied", "User does not have permission to update this listing.");
            if (!room.CanBeEdited())
                return Result<RoomResponseDto>.Failure("InvalidState", "Room cannot be edited in its current state.");
            room.UpdateDetails(dto.Title ?? room.Title, dto.Description ?? room.Description, dto.PricePerNight ?? room.PricePerNight, dto.MaxGuests ?? room.MaxGuests, dto.CheckInTime ?? room.CheckInTime, dto.CheckOutTime ?? room.CheckOutTime, dto.FreeCancellation ?? room.FreeCancellation);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<RoomResponseDto>.Success(new RoomResponseDto
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description,
                RoomType = room.RoomType.ToString(),
                Address = $"{room.Address.Street}, {room.Address.City}, {room.Address.State}, {room.Address.ZipCode}",
                City = room.Address.City,
                PricePerNight = room.PricePerNight,
                MaxGuests = room.MaxGuests,
                FreeCancellation = room.FreeCancellation,
                Status = room.Status.ToString(),
                AverageRating = room.AverageRating,
                Photos = room.Photos.Select(p => new RoomPhotoDto { Id = p.Id, Url = p.Url }).ToList(),
                Amenities = room.Amenities.Select(a => a.Name).ToList()
            });
        }
    }
}