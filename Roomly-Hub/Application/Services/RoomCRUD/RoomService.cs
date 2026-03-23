using Application.Common.Constants;
using Application.Common.Mappers;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Rooms;
using Domain.Interfaces.Repositories;

namespace Application.Services.RoomCRUD
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomMapper _roomMapper;

        public RoomService(
            IRoomRepository roomRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IRoomMapper roomMapper)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _roomMapper = roomMapper;
        }

        public async Task<Result<RoomResponseDto>> CreateRoomAsync(Guid hostId, CreateRoomRequestDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(hostId, cancellationToken);
            if (user == null)
                return Result<RoomResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Room.UserNotFound);

            if (!user.CanCreateListing())
                return Result<RoomResponseDto>.Failure(Errors.Codes.Common.PermissionDenied, Errors.Messages.Room.CannotCreateListing);

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

            return Result<RoomResponseDto>.Success(_roomMapper.ToResponseDto(room));
        }

        public async Task<Result> DeactivateRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            if (room.HostId != hostId)
                return Result.Failure(Errors.Codes.Common.PermissionDenied, Errors.Messages.Room.CannotDeactivateListing);

            if (!room.CanBeDeactivated())
                return Result.Failure(Errors.Codes.Common.InvalidState, Errors.Messages.Room.RoomCannotBeDeactivated);

            room.Deactivate();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<RoomResponseDto>> GetRoomByIdAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result<RoomResponseDto>.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);
            
            return Result<RoomResponseDto>.Success(_roomMapper.ToResponseDto(room));
        }

        public async Task<Result<PagedResult<RoomSummaryDto>>> SearchRoomsAsync(RoomFilters filters, CancellationToken cancellationToken = default)
        {
            var (rooms, totalCount) = await _roomRepository.SearchRoomsAsync(filters, cancellationToken);

            var dtos = rooms.Select(r => _roomMapper.ToSummaryDto(r)).ToList();

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
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            if (room.HostId != hostId)
                return Result.Failure(Errors.Codes.Common.PermissionDenied, Errors.Messages.Room.CannotSubmitForReview);

            room.SubmitForReview();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<RoomResponseDto>> UpdateRoomAsync(Guid hostId, Guid roomId, UpdateRoomRequestDto dto, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result<RoomResponseDto>.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            if (room.HostId != hostId)
                return Result<RoomResponseDto>.Failure(Errors.Codes.Common.PermissionDenied, Errors.Messages.Room.CannotUpdateListing);

            if (!room.CanBeEdited())
                return Result<RoomResponseDto>.Failure(Errors.Codes.Common.InvalidState, Errors.Messages.Room.RoomCannotBeEdited);
            
            room.UpdateDetails(
                dto.Title ?? room.Title, 
                dto.Description ?? room.Description, 
                dto.PricePerNight ?? room.PricePerNight, 
                dto.MaxGuests ?? room.MaxGuests, 
                dto.CheckInTime ?? room.CheckInTime, 
                dto.CheckOutTime ?? room.CheckOutTime, 
                dto.FreeCancellation ?? room.FreeCancellation);
            
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<RoomResponseDto>.Success(_roomMapper.ToResponseDto(room));
        }
    }
}