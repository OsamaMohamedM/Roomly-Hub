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
    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IHasher _hasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<LoginRequestDto> _validator;
        private readonly JwtSettings _jwtSettings;

        public LoginService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            IValidator<LoginRequestDto> validator,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", ValidationHelper.ToErrorDictionary(validationResult));

            Email email;
            try
            {
                email = Email.Create(requestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null)
                return Result<LoginResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");

            if (!_hasher.Verify(requestDto.Password, user.PasswordHash))
            {
                if (!user.IsLocked)
                {
                    user.IncrementLoginFailCount();
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return Result<LoginResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");
            }

            // Auto-unlock if admin-set time-based lockout has expired
            if (user.IsLocked && user.LockoutTokenExpiresAt.HasValue && user.LockoutTokenExpiresAt < DateTime.UtcNow)
            {
                user.Unlock();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (user.IsLocked)
                return Result<LoginResponseDto>.Failure("ACCOUNT_LOCKED", "Your account is locked. Please contact support.");

            if (!user.EmailVerified)
                return Result<LoginResponseDto>.Failure("EMAIL_NOT_VERIFIED", "Please verify your email before logging in.");

            if (!user.IsActive)
                return Result<LoginResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            var accessToken = await _tokenService.GenerateAccessToken(user);
            var refreshTokenString = await _tokenService.GenerateRefreshTokenAsync();

            user.ResetLoginFailCount();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            user.AddRefreshToken(RefreshToken.Create(
                user.Id,
                _hasher.HashToken(refreshTokenString),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(
                new LoginResponseDto(accessToken, refreshTokenString, "Login successful."));
        }
    }
}