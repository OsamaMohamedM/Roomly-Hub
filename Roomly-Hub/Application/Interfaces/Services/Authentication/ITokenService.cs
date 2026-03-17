using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(User user);

        Task<string> GenerateRefreshTokenAsync();
    }
}