using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<UpdateProfileRequestDto> _validator;

        public UserService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IValidator<UpdateProfileRequestDto> validator)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Result<UserProfileResponseDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", "User ID is required.");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<UserProfileResponseDto>.Failure("USER_NOT_FOUND", "User not found.");

            return Result<UserProfileResponseDto>.Success(Map(user));
        }

        public async Task<Result<UserProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            if (userId == Guid.Empty)
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", "User ID is required.");

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", ValidationHelper.ToErrorDictionary(validationResult));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<UserProfileResponseDto>.Failure("USER_NOT_FOUND", "User not found.");

            try
            {
                user.UpdateProfile(requestDto.Name, requestDto.Bio);

                if (!string.IsNullOrWhiteSpace(requestDto.PhoneNumber))
                    user.SetPhoneNumber(PhoneNumber.Create(requestDto.PhoneNumber));

                if (!string.IsNullOrWhiteSpace(requestDto.ProfilePhotoUrl))
                    user.SetProfilePhoto(requestDto.ProfilePhotoUrl);
            }
            catch (ArgumentException ex)
            {
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<UserProfileResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<UserProfileResponseDto>.Success(Map(user));
        }

        private static UserProfileResponseDto Map(Domain.Entities.User user)
        {
            return new UserProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email.Value,
                PhoneNumber = user.PhoneNumber?.Value,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                Bio = user.Bio,
                EmailVerified = user.EmailVerified,
                KycStatus = user.KycStatus,
                KycAttemptCount = user.KycAttemptCount,
                Role = user.Role,
                AdminRole = user.AdminRole,
                IsActive = user.IsActive,
                IsLocked = user.IsLocked,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
