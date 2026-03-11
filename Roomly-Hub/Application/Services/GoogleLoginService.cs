using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class GoogleLoginService : IGoogleLoginService
    {
        private const string GoogleProvider = "Google";

        private readonly IUserRepository _userRepository;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ITokenService _tokenService;
        private readonly IHasher _hasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<GoogleAuthRequestDto> _validator;
        private readonly JwtSettings _jwtSettings;

        public GoogleLoginService(
            IUserRepository userRepository,
            IGoogleAuthService googleAuthService,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            IValidator<GoogleAuthRequestDto> validator,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _googleAuthService = googleAuthService;
            _tokenService = tokenService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<LoginResponseDto>> LoginWithGoogleAsync(GoogleAuthRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", ValidationHelper.ToErrorDictionary(validationResult));

            var googleUser = await _googleAuthService.ValidateIdTokenAsync(requestDto.IdToken, cancellationToken);
            if (googleUser is null)
                return Result<LoginResponseDto>.Failure("INVALID_GOOGLE_TOKEN", "Invalid or expired Google token.");

            if (!googleUser.EmailVerified)
                return Result<LoginResponseDto>.Failure("EMAIL_NOT_VERIFIED", "Google account email is not verified.");

            Email email;
            try
            {
                email = Email.Create(googleUser.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            bool isNewUser = false;
            User user;

            var existingByGoogleId = await _userRepository.GetByExternalLoginAsync(GoogleProvider, googleUser.GoogleId, cancellationToken);

            if (existingByGoogleId is not null)
            {
                user = existingByGoogleId;
            }
            else
            {
                var existingByEmail = await _userRepository.GetByEmailWithExternalLoginsAsync(email, cancellationToken);

                if (existingByEmail is not null)
                {
                    user = existingByEmail;
                    user.AddExternalLogin(UserExternalLogin.Create(user.Id, GoogleProvider, googleUser.GoogleId, email));

                    if (!user.EmailVerified)
                        user.VerifyEmail();
                }
                else
                {
                    isNewUser = true;
                    var placeholderPasswordHash = _hasher.Hash(Guid.NewGuid().ToString());
                    user = User.CreateFromExternalLogin(googleUser.Name, email, placeholderPasswordHash);
                    user.AddExternalLogin(UserExternalLogin.Create(user.Id, GoogleProvider, googleUser.GoogleId, email));
                    await _userRepository.AddAsync(user, cancellationToken);
                }
            }

            if (!user.IsActive)
                return Result<LoginResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            if (user.IsLocked)
                return Result<LoginResponseDto>.Failure("ACCOUNT_LOCKED", "Your account is locked. Please contact support.");

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = await _tokenService.GenerateAccessToken(user);
            var refreshTokenString = await _tokenService.GenerateRefreshTokenAsync();

            user.AddRefreshToken(RefreshToken.Create(
                user.Id,
                _hasher.HashToken(refreshTokenString),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(
                new LoginResponseDto(accessToken, refreshTokenString, "Login with Google successful."));
        }
    }
}