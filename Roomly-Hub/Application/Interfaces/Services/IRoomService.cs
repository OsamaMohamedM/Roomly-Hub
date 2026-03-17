using Application.Common.Filters;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IRoomService
    {
        Task<Result<RoomResponseDto>> CreateRoomAsync(Guid hostId, CreateRoomRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result<RoomResponseDto>> UpdateRoomAsync(Guid hostId, Guid roomId, UpdateRoomRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result> SubmitForReviewAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result> DeactivateRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result<RoomResponseDto>> GetRoomByIdAsync(Guid roomId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<RoomSummaryDto>>> SearchRoomsAsync(RoomFilters filters);
    }
}