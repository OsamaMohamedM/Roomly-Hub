using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IHasher _hasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<RefreshTokenRequestDto> _validator;
        private readonly JwtSettings _jwtSettings;

        public RefreshTokenService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IHasher hasher,
            IUnitOfWork unitOfWork,
            IValidator<RefreshTokenRequestDto> validator,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _hasher = hasher;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", ValidationHelper.ToErrorDictionary(validationResult));

            var tokenHash = _hasher.HashToken(requestDto.RefreshToken);

            var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
            if (user is null)
                return Result<LoginResponseDto>.Failure("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token.");

            var refreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.TokenHash == tokenHash);
            if (refreshToken is null || !refreshToken.IsActive())
                return Result<LoginResponseDto>.Failure("INVALID_REFRESH_TOKEN", "Invalid or expired refresh token.");

            if (!user.IsActive)
                return Result<LoginResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            if (user.IsLocked)
                return Result<LoginResponseDto>.Failure("ACCOUNT_LOCKED", "Your account is locked.");

            // Rotate: revoke old token and issue new pair
            refreshToken.Revoke();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var newAccessToken = await _tokenService.GenerateAccessToken(user);
            var newRefreshTokenString = await _tokenService.GenerateRefreshTokenAsync();

            user.AddRefreshToken(RefreshToken.Create(
                user.Id,
                _hasher.HashToken(newRefreshTokenString),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(
                new LoginResponseDto(newAccessToken, newRefreshTokenString, "Token refreshed successfully."));
        }
    }
}
