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

        Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task<User?> GetByExternalLoginAsync(string provider, string externalId, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailWithExternalLoginsAsync(Email email, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
    }
}