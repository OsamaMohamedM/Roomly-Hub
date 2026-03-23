using Application.Common.Constants;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Rooms;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Enums;
using Domain.Interfaces.Repositories;

namespace Application.Services.RoomCRUD
{
    public class RoomModerationService : IRoomModerationService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomMapper _roomMapper;

        public RoomModerationService(
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

        public async Task<Result> ApproveRoomAsync(Guid moderatorId, Guid roomId, CancellationToken cancellationToken = default)
        {
            var moderator = await _userRepository.GetByIdAsync(moderatorId, cancellationToken);
            if (moderator == null)
            {
                return Result.Failure(RoomErrorCodes.ModeratorNotFound, RoomErrorMessages.ModeratorNotFoundMessage);
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure(RoomErrorCodes.UnauthorizedAction, RoomErrorMessages.UnauthorizedActionMessage);
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure(RoomErrorCodes.RoomNotFound, RoomErrorMessages.RoomNotFoundMessage);
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure(RoomErrorCodes.InvalidRoomStatus, RoomErrorMessages.OnlyPendingRoomsCanBeApprovedMessage);
            }

            room.Approve();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<List<PendingRoomDto>>> GetPendingRoomsAsync(CancellationToken cancellationToken = default)
        {
            var rooms = await _roomRepository.GetPendingReviewRoomsAsync(cancellationToken);
            var pendingRoomDTOs = rooms.Select(r => _roomMapper.ToPendingRoomDto(r)).ToList();
            return Result<List<PendingRoomDto>>.Success(pendingRoomDTOs);
        }

        public async Task<Result> RejectRoomAsync(Guid moderatorId, Guid roomId, string reason, CancellationToken cancellationToken = default)
        {
            var moderator = await _userRepository.GetByIdAsync(moderatorId, cancellationToken);
            if (moderator == null)
            {
                return Result.Failure(RoomErrorCodes.ModeratorNotFound, RoomErrorMessages.ModeratorNotFoundMessage);
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure(RoomErrorCodes.UnauthorizedAction, RoomErrorMessages.UnauthorizedActionMessage);
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure(RoomErrorCodes.RoomNotFound, RoomErrorMessages.RoomNotFoundMessage);
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure(RoomErrorCodes.InvalidRoomStatus, RoomErrorMessages.OnlyPendingRoomsCanBeRejectedMessage);
            }

            room.Reject(reason);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}