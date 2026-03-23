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
                return Result.Failure(Errors.Codes.Room.ModeratorNotFound, Errors.Messages.Room.ModeratorNotFound);
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure(Errors.Codes.Room.InvalidRoomStatus, Errors.Messages.Room.OnlyPendingRoomsCanBeApproved);
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
                return Result.Failure(Errors.Codes.Room.ModeratorNotFound, Errors.Messages.Room.ModeratorNotFound);
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure(Errors.Codes.Room.RoomNotFound, Errors.Messages.Room.RoomNotFound);
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure(Errors.Codes.Room.InvalidRoomStatus, Errors.Messages.Room.OnlyPendingRoomsCanBeRejected);
            }

            room.Reject(reason);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}