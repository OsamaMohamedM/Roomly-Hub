using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

        Task<User?> GetByIdWithOtpsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailWithOtpsAsync(Email email, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        void Update(User user);
        void Delete(User user);
    }
}
