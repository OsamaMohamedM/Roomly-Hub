using Application.Common.Results;
using Application.DTOs.Rooms;

namespace Application.Interfaces.Services
{
    public interface IRoomModerationService
    {
        Task<Result<List<PendingRoomDto>>> GetPendingRoomsAsync(CancellationToken cancellationToken = default);

        Task<Result> ApproveRoomAsync(Guid moderatorId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result> RejectRoomAsync(Guid moderatorId, Guid roomId, string reason, CancellationToken cancellationToken = default);
    }
}