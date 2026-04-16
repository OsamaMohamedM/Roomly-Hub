using Application.Common.Results;
using Application.DTOs.Reviews;

namespace Application.Interfaces.Services.Reviews
{
    public interface IReviewCommandService
    {
        public Task<Result<ReviewResponseDto>> SubmitReviewAsync(Guid reviewerId, SubmitReviewRequestDto dto, CancellationToken ct);

        public Task<Result> FlagReviewAsync(Guid moderatorId, Guid reviewId, FlagReviewRequestDto dto, CancellationToken ct);

        public Task<Result> RemoveReviewAsync(Guid moderatorId, Guid reviewId, CancellationToken ct);
    }
}