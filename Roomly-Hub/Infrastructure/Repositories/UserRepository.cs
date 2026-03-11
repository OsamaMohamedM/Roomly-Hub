using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbSet<User> _dbSet;

        public UserRepository(AppDbContext context)
        {
            _dbSet = context.Set<User>();
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByIdWithOtpsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Otps)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByEmailWithOtpsAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Otps)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(user, cancellationToken);
        }

        public void Update(User user)
        {
            user.MarkUpdated();
            _dbSet.Update(user);
        }

        public void Delete(User user)
        {
            user.SoftDelete();
            _dbSet.Update(user);
        }
    }
}
