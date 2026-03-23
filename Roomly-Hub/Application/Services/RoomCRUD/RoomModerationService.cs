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

        public RoomModerationService(IRoomRepository roomRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> ApproveRoomAsync(Guid moderatorId, Guid roomId, CancellationToken cancellationToken = default)
        {
            // Validate moderator has proper authorization
            var moderator = await _userRepository.GetByIdAsync(moderatorId, cancellationToken);
            if (moderator == null)
            {
                return Result.Failure("ModeratorNotFound", "The specified moderator was not found.");
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure("UnauthorizedAction", "User does not have permission to approve rooms.");
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure("RoomNotFound", "The specified room does not exist.");
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure("InvalidRoomStatus", "Only rooms that are pending review can be approved.");
            }

            room.Approve();
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<List<PendingRoomDto>>> GetPendingRoomsAsync(CancellationToken cancellationToken = default)
        {
            var rooms = await _roomRepository.GetPendingReviewRoomsAsync(cancellationToken);
            var pendingRoomDTOs = rooms.Select(r => new PendingRoomDto
            {
                Id = r.Id,
                HostId = r.HostId,
                SubmittedAt = r.CreatedAt,
                Room = r
            }).ToList();
            return Result<List<PendingRoomDto>>.Success(pendingRoomDTOs);
        }

        public async Task<Result> RejectRoomAsync(Guid moderatorId, Guid roomId, string reason, CancellationToken cancellationToken = default)
        {
            // Validate moderator has proper authorization
            var moderator = await _userRepository.GetByIdAsync(moderatorId, cancellationToken);
            if (moderator == null)
            {
                return Result.Failure("ModeratorNotFound", "The specified moderator was not found.");
            }

            if (!moderator.HasAdminRole(AdminRole.Moderator) && !moderator.HasAdminRole(AdminRole.SuperAdmin))
            {
                return Result.Failure("UnauthorizedAction", "User does not have permission to reject rooms.");
            }

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure("RoomNotFound", "The specified room does not exist.");
            }

            if (room.Status != Domain.enums.Room.RoomListingStatus.PendingReview)
            {
                return Result.Failure("InvalidRoomStatus", "Only rooms that are pending review can be rejected.");
            }

            room.Reject(reason);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}