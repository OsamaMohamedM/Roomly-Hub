using Application.Common.Results;
using Application.DTOs.Reviews;

namespace Application.Interfaces.Services.Reviews
{
    public interface IReviewQueryService
    {
        public Task<Result<List<ReviewResponseDto>>> GetRoomReviewsAsync(Guid roomId, CancellationToken ct);

        public Task<Result<List<ReviewResponseDto>>> GetUserReviewsAsync(Guid userId, CancellationToken ct);
    }
}