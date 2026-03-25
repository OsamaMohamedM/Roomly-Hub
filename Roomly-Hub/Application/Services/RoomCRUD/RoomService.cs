using Application.Common.Constants;
using Application.Common.Mappers;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Rooms;
using Domain.enums.Room;
using Domain.Interfaces.Repositories;
using FluentValidation;

namespace Application.Services.RoomCRUD
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomMapper _roomMapper;
        private readonly IValidator<BlockDatesRequestDto> _blockDatesRequestValidator;

        public RoomService(
            IRoomRepository roomRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IRoomMapper roomMapper,
            IValidator<BlockDatesRequestDto> blockDatesRequestValidator)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _roomMapper = roomMapper;
            _blockDatesRequestValidator = blockDatesRequestValidator;
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


        public async Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(Guid roomId, int year, int month, CancellationToken cancellationToken = default)
        {
            if (month is < 1 or > 12)
                return Result<AvailabilityResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result<AvailabilityResponseDto>.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            var blockedDates = await _roomRepository.GetBlockedDatesAsync(roomId, year, month, cancellationToken);

            return Result<AvailabilityResponseDto>.Success(new AvailabilityResponseDto
            {
                RoomId = roomId,
                BlockedDates = blockedDates.Select(x => x.BlockedDate).OrderBy(x => x).ToList()
            });
        }

        public async Task<Result> BlockDatesAsync(Guid hostId, Guid roomId, BlockDatesRequestDto dto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _blockDatesRequestValidator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            if (room.HostId != hostId)
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);

            if (room.Status is not RoomListingStatus.Published and not RoomListingStatus.Inactive)
                return Result.Failure(Errors.Codes.Common.InvalidState, Errors.Messages.Room.RoomCannotBeEdited);

            for (var date = dto.From; date <= dto.To; date = date.AddDays(1))
            {
                if (room.Availabilities.Any(a => a.BlockedDate == date && !a.IsDeleted))
                    return Result.Failure(Errors.Codes.Room.DateAlreadyBlocked, Errors.Messages.Room.DateAlreadyBlocked);
            }

            room.BlockDateRange(dto.From, dto.To, AvailabilityReason.HostBlocked);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        public async Task<Result> UnblockDatesAsync(Guid hostId, Guid roomId, BlockDatesRequestDto dto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _blockDatesRequestValidator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);

            if (room.HostId != hostId)
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);

            for (var date = dto.From; date <= dto.To; date = date.AddDays(1))
            {
                if (!room.Availabilities.Any(a => a.BlockedDate == date && !a.IsDeleted))
                    return Result.Failure(Errors.Codes.Room.DateNotBlocked, Errors.Messages.Room.DateNotBlocked);
            }

            room.UnblockDateRange(dto.From, dto.To);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}