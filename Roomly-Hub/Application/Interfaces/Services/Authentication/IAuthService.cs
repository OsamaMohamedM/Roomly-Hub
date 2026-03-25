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

        Task<Result<TokenResponseDto>> GenerateNewAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

        public Task<Result> ForgotPasswordAsync(string userEmail, CancellationToken cancellationToken);

        public Task<Result> ResetPasswordAsync(ResetPasswordDto dto);

        Task<Result> RequestAccountUnlockAsync(RequestUnlockDto dto, CancellationToken cancellationToken);

        Task<Result> UnlockAccountAsync(UnlockAccountDto dto, CancellationToken cancellationToken);
    }
}