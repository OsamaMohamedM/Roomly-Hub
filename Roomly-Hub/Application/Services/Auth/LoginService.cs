using Application.Common.Constants;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Events.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly IPublisher _publisher;
        private readonly ILogger<LoginService> _logger;

        public LoginService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            IValidator<LoginRequestDto> validator,
            IOptions<JwtSettings> jwtSettings,
            IPublisher publisher,
            ILogger<LoginService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _jwtSettings = jwtSettings.Value;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));

            Email email;
            try
            {
                email = Email.Create(requestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<LoginResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Auth.InvalidCredentials, Errors.Messages.Auth.InvalidCredentials);

            if (!_hasher.Verify(requestDto.Password, user.PasswordHash))
            {
                if (!user.IsLocked)
                {
                    user.IncrementLoginFailCount(TimeSpan.FromMinutes(_jwtSettings.AccountLockoutMinutes));
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (user.IsLocked)
                    {
                        try
                        {
                            await _publisher.Publish(new AccountLockedEvent(user.Id), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish AccountLockedEvent for user {UserId}", user.Id);
                        }
                    }
                }
                return Result<LoginResponseDto>.Failure(Errors.Codes.Auth.InvalidCredentials, Errors.Messages.Auth.InvalidCredentials);
            }

            if (user.IsLocked && user.LockoutTokenExpiresAt.HasValue && user.LockoutTokenExpiresAt < DateTime.UtcNow)
            {
                user.Unlock();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (user.IsLocked)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Auth.AccountLocked, Errors.Messages.Auth.AccountLocked);

            if (!user.EmailVerified)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Auth.EmailNotVerified, Errors.Messages.Auth.EmailNotVerified);

            if (!user.IsActive)
                return Result<LoginResponseDto>.Failure(Errors.Codes.Auth.AccountInactive, Errors.Messages.Auth.AccountInactive);

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
