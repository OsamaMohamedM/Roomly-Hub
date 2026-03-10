using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DbSet<User> _dbSet;

        public UserRepository(AppDbContext context)
        {
            _dbSet = context.Set<User>();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(Email email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task AddAsync(User user)
        {
            await _dbSet.AddAsync(user);
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