using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens.Where(t => !t.RevokedAt.HasValue && t.ExpiresAt > DateTime.UtcNow))
                .Include(u => u.Otps)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByIdWithOtpsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Otps)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByEmailWithOtpsAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Otps)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(
                    u => u.RefreshTokens.Any(rt => rt.TokenHash == tokenHash) && !u.IsDeleted,
                    cancellationToken);
        }

        public async Task<User?> GetByExternalLoginAsync(string provider, string externalId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(
                    u => u.ExternalLogins.Any(el => el.Provider == provider && el.ExternalId == externalId)
                         && !u.IsDeleted,
                    cancellationToken);
        }

        public async Task<User?> GetByEmailWithExternalLoginsAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.ExternalLogins)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }
    }
}