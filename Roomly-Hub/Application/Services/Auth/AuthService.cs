using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;

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
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        public AuthService(
            IRegisterService registerService,
            ILoginService loginService,
            IRefreshTokenService refreshTokenService,
            IGoogleLoginService googleLoginService,
            IUserRepository userRepository,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IOtpService otpService,

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
            _emailService = emailService;
            _otpService = otpService;
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
            var tokenHash = _hasher.HashToken(refreshToken);

            var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash);
            if (user is null)
                return Result<TokenResponseDto>.Failure("USER_NOT_FOUND", "refresh not found.");

            if (!user.IsActive)
                return Result<TokenResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            if (user.IsLocked)
                return Result<TokenResponseDto>.Failure("ACCOUNT_LOCKED", "Your account is locked.");
            if (user.IsRefreshTokenRevoked(tokenHash))
            {
                user.RevokeAllRefreshTokens();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<TokenResponseDto>.Failure("SECURITY_ALERT", "Token reuse detected. Please login again.");
            }
            if (user.IsRefreshTokenExpired(tokenHash))
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

        public async Task<Result> ForgotPasswordAsync(string userEmail, CancellationToken cancellationToken)
        {
            if (userEmail == null || userEmail.Length == 0)
            {
                return Result.Failure("VALIDATION_ERROR", "Email is required.");
            }
            var email = Email.Create(userEmail);

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null)
                return Result.Success();

            user.InvalidatePreviousOtps(OtpPurpose.PasswordReset);
            var otp = Otp.Create(user.Id, _otpService.GenerateOtp(), OtpPurpose.PasswordReset, DateTime.UtcNow.AddMinutes(25));
            user.AddOtp(otp);
            await _unitOfWork.SaveChangesAsync();
            try
            {
                await _emailService.SendEmailAsync(
                   user.Email.Value,
                   "Password Reset- Roomly",
                   $"<h1>Welcome, {user.Name}!</h1><p>Your Otp code is: <b>{otp.CodeHash}</b></p><p>This code will expire in {otp.ExpiresAt} minutes.</p>",
                   cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var email = Email.Create(dto.Email);
            var user = await _userRepository.GetByEmailAsync(email);
            if (user is null)
                return Result.Failure("INVALID_REQUEST", "Try again.");

            var otpHash = _hasher.Hash(dto.OtpCode);
            var otp = Otp.Create(user.Id, dto.OtpCode, OtpPurpose.PasswordReset, DateTime.UtcNow);
            if (!user.CheckValidOtp(otp))
            {
                return Result.Failure("INVALID_REQUEST", "Try again.");
            }
            user.RevokeOtp(otp);
            var newHash = _hasher.Hash(dto.Password);
            user.SetPasswordHash(newHash);
            user.RevokeAllRefreshTokens();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}