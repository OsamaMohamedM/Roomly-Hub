using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRegisterService _registerService;
        private readonly ILoginService _loginService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IGoogleLoginService _googleLoginService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IHasher _hasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IRegisterService registerService,
            ILoginService loginService,
            IRefreshTokenService refreshTokenService,
            IGoogleLoginService googleLoginService,
            IUserRepository userRepository,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
        {
            _registerService = registerService;
            _loginService = loginService;
            _refreshTokenService = refreshTokenService;
            _googleLoginService = googleLoginService;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
        }

        public Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            return _registerService.RegisterAsync(requestDto, cancellationToken);
        }

        public Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            return _loginService.LoginAsync(requestDto, cancellationToken);
        }

        public Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            return _refreshTokenService.RefreshTokenAsync(requestDto, cancellationToken);
        }

        public Task<Result<LoginResponseDto>> OAuthWithGoogleAsync(GoogleAuthRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            return _googleLoginService.LoginWithGoogleAsync(requestDto, cancellationToken);
        }

        public async Task<Result<TokenResponseDto>> GenerateNewAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken == String.Empty)
                return Result<TokenResponseDto>.Failure("VALIDATION_ERROR", "User ID is required.");

            var user = await _userRepository.GetByRefreshTokenHashAsync(refreshToken);
            if (user is null)
                return Result<TokenResponseDto>.Failure("USER_NOT_FOUND", "refresh not found.");

            if (!user.IsActive)
                return Result<TokenResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            if (user.IsLocked)
                return Result<TokenResponseDto>.Failure("ACCOUNT_LOCKED", "Your account is locked.");
            if (user.IsRefreshTokenRevoked(refreshToken))
            {
                user.RevokeAllRefreshTokens();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<TokenResponseDto>.Failure("SECURITY_ALERT", "Token reuse detected. Please login again.");
            }
            if (user.IsRefreshTokenExpired(refreshToken))
                return Result<TokenResponseDto>.Failure("TOKEN_EXPIRED", "Your session has expired. Please login again.");

            var newAccessToken = await _tokenService.GenerateAccessToken(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
            user.RevokeRefreshToken(newRefreshToken);
            user.AddRefreshToken(RefreshToken.Create(
                user.Id,
                _hasher.HashToken(newRefreshToken),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TokenResponseDto>.Success(new TokenResponseDto(newAccessToken, newRefreshToken));
        }

        public async Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                return Result.Failure("VALIDATION_ERROR", "User ID is required.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result.Failure("USER_NOT_FOUND", "User not found.");
            user.RevokeAllRefreshTokens();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Result>.Success();
        }

        public Task<Result> PasswordResetAsync(CancellationToken cancellationToken = default)
        {
        }
    }
}