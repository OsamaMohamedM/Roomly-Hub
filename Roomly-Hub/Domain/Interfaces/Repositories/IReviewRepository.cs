using Domain.Entities.Reviews;
using Domain.enums.Reviews;

namespace Domain.Interfaces.Repositories
{
    public interface IReviewRepository
    {
        Task<Review?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<bool> ExistsAsync(Guid bookingId, Guid reviewerId, ReviewType type, CancellationToken ct);

        Task<List<Review>> GetByRoomIdAsync(Guid roomId, CancellationToken ct);
        Task<(List<Review> Reviews, int TotalCount)> GetVisibleByRoomIdPagedAsync(Guid roomId, int page, int pageSize, CancellationToken ct);

        Task<List<Review>> GetBySubjectUserIdAsync(Guid userId, CancellationToken ct);
        Task<(List<Review> Reviews, int TotalCount)> GetVisibleBySubjectUserIdPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct);

        Task<List<Review>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken ct);

        Task AddAsync(Review review, CancellationToken ct);

        void Update(Review review);
    }
}
