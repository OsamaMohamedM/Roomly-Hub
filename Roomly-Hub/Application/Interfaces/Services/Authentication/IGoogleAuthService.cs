using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
