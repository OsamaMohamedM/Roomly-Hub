using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(Email email);
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
    }
}