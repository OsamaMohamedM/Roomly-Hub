using Application.Common.Constants;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using FluentValidation;

namespace Application.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<OtpVerifyDto> _validator;

        public EmailVerificationService(
            IUserRepository userRepository,
            IOtpService otpService,
            IUnitOfWork unitOfWork,
            IValidator<OtpVerifyDto> validator)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Result<EmailVerifyResponseDto>> VerifyEmailAsync(OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken = default)
        {
            if (otpVerifyDto is null)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            var validationResult = await _validator.ValidateAsync(otpVerifyDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));

            var user = await _userRepository.GetByIdWithOtpsAsync(otpVerifyDto.UserId, cancellationToken);
            if (user is null)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

            if (user.EmailVerified)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Auth.EmailAlreadyVerified, Errors.Messages.Auth.EmailAlreadyVerified);

            var validOtps = user.Otps
                .Where(o => o.Purpose == OtpPurpose.EmailVerification && !o.IsExpired() && !o.IsUsed())
                .ToList();

            if (validOtps.Count == 0)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Auth.InvalidOtp, "No valid OTP found for this user.");

            var matchedOtp = validOtps.FirstOrDefault(o => _otpService.VerifyOtp(otpVerifyDto.Otp, o.CodeHash));
            if (matchedOtp is null)
                return Result<EmailVerifyResponseDto>.Failure(Errors.Codes.Auth.InvalidOtp, Errors.Messages.Auth.InvalidOtp);

            matchedOtp.MarkAsUsed();
            user.VerifyEmail();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<EmailVerifyResponseDto>.Success(new EmailVerifyResponseDto("Email verified successfully."));
        }
    }
}