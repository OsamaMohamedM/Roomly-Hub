using Domain.Entities.Reviews;
using Domain.enums.Reviews;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Review review, CancellationToken ct)
        {
            await _context.Reviews.AddAsync(review, ct);
        }

        public async Task<bool> ExistsAsync(Guid bookingId, Guid reviewerId, ReviewType type, CancellationToken ct)
        {
            return await _context.Reviews
                .AnyAsync(r => r.BookingId == bookingId && r.ReviewerId == reviewerId && r.Type == type && !r.IsDeleted, ct);
        }

        public async Task<Review?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        }

        public async Task<List<Review>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken ct)
        {
            return await _context.Reviews
                .Where(r => r.ReviewerId == reviewerId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Review>> GetByRoomIdAsync(Guid roomId, CancellationToken ct)
        {
            return await _context.Reviews
                .Where(r => r.SubjectRoomId == roomId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<Review>> GetBySubjectUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Reviews
                .Where(r => r.SubjectUserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }

        public void Update(Review review)
        {
            _context.Reviews.Update(review);
        }
    }
}