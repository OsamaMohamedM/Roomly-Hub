using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<LoginResponseDto>> OAuthWithGoogleAsync(GoogleAuthRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<TokenResponseDto>> GenerateNewAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> PasswordResetAsync(CancellationToken cancellationToken = default);
    }
}