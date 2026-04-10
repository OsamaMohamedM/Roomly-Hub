using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Domain.Entities.Rooms;

namespace Application.Interfaces.Services
{
    public interface IRoomService
    {
        Task<Result<RoomResponseDto>> CreateRoomAsync(Guid hostId, CreateRoomRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result<RoomResponseDto>> UpdateRoomAsync(Guid hostId, Guid roomId, UpdateRoomRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result> SubmitForReviewAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result> DeactivateRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result> ActivateRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result> DeleteRoomAsync(Guid hostId, Guid roomId, CancellationToken cancellationToken = default);

        Task<Result<RoomPhotoDto>> AddRoomPhotoAsync(Guid hostId, Guid roomId, AddRoomPhotoRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result> RemoveRoomPhotoAsync(Guid hostId, Guid roomId, Guid photoId, CancellationToken cancellationToken = default);

        Task<Result<RoomResponseDto>> GetRoomByIdAsync(Guid roomId, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<RoomSummaryDto>>> SearchRoomsAsync(RoomFilters filters, CancellationToken cancellationToken = default);
        Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(Guid roomId, int year, int month, CancellationToken cancellationToken = default);


        Task<Result> BlockDatesAsync(Guid hostId, Guid roomId, BlockDatesRequestDto dto, CancellationToken cancellationToken = default);

        Task<Result> UnblockDatesAsync(Guid hostId, Guid roomId, BlockDatesRequestDto dto, CancellationToken cancellationToken = default);
    }
}