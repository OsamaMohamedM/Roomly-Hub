using Application.Common.Constants;
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
            if (refreshToken == string.Empty)
                return Result<TokenResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Auth.InvalidRefreshToken);

            var tokenHash = _hasher.HashToken(refreshToken);

            var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash);
            if (user is null)
                return Result<TokenResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Auth.RefreshTokenNotFound);

            if (!user.IsActive)
                return Result<TokenResponseDto>.Failure(Errors.Codes.Auth.AccountInactive, Errors.Messages.Auth.AccountInactive);

            if (user.IsLocked)
                return Result<TokenResponseDto>.Failure(Errors.Codes.Auth.AccountLocked, Errors.Messages.Auth.AccountLocked);

            if (user.IsRefreshTokenRevoked(tokenHash))
            {
                user.RevokeAllRefreshTokens();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<TokenResponseDto>.Failure(Errors.Codes.Auth.SecurityAlert, Errors.Messages.Auth.SecurityAlert);
            }

            if (user.IsRefreshTokenExpired(tokenHash))
                return Result<TokenResponseDto>.Failure(Errors.Codes.Auth.TokenExpired, Errors.Messages.Auth.TokenExpired);

            user.RevokeRefreshToken(tokenHash);
            var newAccessToken = await _tokenService.GenerateAccessToken(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();
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
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.UserIdRequired);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

            user.RevokeAllRefreshTokens();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ForgotPasswordAsync(string userEmail, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.EmailRequired);
            }

            var email = Email.Create(userEmail);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null)
                return Result.Success();

            user.InvalidatePreviousOtps(OtpPurpose.PasswordReset);
            var otpCode = _otpService.GenerateOtp();
            var otp = Otp.Create(user.Id, _hasher.Hash(otpCode), OtpPurpose.PasswordReset, DateTime.UtcNow.AddMinutes(25));
            user.AddOtp(otp);
            await _unitOfWork.SaveChangesAsync();
            try
            {
                await _emailService.SendEmailAsync(
                   user.Email.Value,
                   "Password Reset- Roomly",
                   $"<h1>Welcome, {user.Name}!</h1><p>Your Otp code is: <b>{otpCode}</b></p><p>This code will expire in 25 minutes.</p>",
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
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OtpCode) || string.IsNullOrWhiteSpace(dto.Password))
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var email = Email.Create(dto.Email);
            var user = await _userRepository.GetByEmailWithOtpsAsync(email);
            if (user is null)
                return Result.Failure(Errors.Codes.Auth.InvalidRequest, Errors.Messages.Auth.InvalidRequest);
            var validOtps = user.GetValidOtps(OtpPurpose.PasswordReset);
            var matchedOtp = validOtps.FirstOrDefault(o => _hasher.Verify(dto.OtpCode, o.CodeHash));
            if (matchedOtp is null)
                return Result.Failure(Errors.Codes.Auth.InvalidOtp, Errors.Messages.Auth.InvalidOtp);

            matchedOtp.MarkAsUsed();
            var newHash = _hasher.Hash(dto.Password);
            user.SetPasswordHash(newHash);
            user.RevokeAllRefreshTokens();
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}