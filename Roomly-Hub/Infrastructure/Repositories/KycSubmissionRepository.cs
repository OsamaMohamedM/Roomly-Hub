using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class KycSubmissionRepository : IKycSubmissionRepository
    {
        private readonly AppDbContext _context;

        public KycSubmissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<KycSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.KycSubmissions
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        }

        public async Task<KycSubmission?> GetPendingAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.KycSubmissions
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == SubmissionStatus.Pending && !x.IsDeleted, cancellationToken);
        }

        public async Task<IReadOnlyList<KycSubmission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.KycSubmissions
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<KycSubmission>> GetPendingSubmissionsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.KycSubmissions
                .Where(x => x.Status == SubmissionStatus.Pending && !x.IsDeleted)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(KycSubmission submission, CancellationToken cancellationToken = default)
        {
            await _context.KycSubmissions.AddAsync(submission, cancellationToken);
        }
    }
}