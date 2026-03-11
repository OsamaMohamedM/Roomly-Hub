using Domain.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface IKycSubmissionRepository
    {
        Task<KycSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<KycSubmission?> GetPendingAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KycSubmission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(KycSubmission submission, CancellationToken cancellationToken = default);
    }
}
