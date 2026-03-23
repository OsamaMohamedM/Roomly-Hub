using Application.Common.Constants;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Services
{
    public class RegisterService : IRegisterService
    {
        private const int OtpCooldownSeconds = 60;
        private const int OtpExpirationMinutes = 25;

        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IHasher _hasher;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<RegisterRequestDto> _validator;

        public RegisterService(
            IUserRepository userRepository,
            IEmailService emailService,
            IHasher hasher,
            IOtpService otpService,
            IUnitOfWork unitOfWork,
            IValidator<RegisterRequestDto> validator)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _hasher = hasher;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _validator = validator;
        }

        public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<RegisterResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            var validationResult = await _validator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<RegisterResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));

            Email email;
            try
            {
                email = Email.Create(requestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<RegisterResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }

            var existingUser = await _userRepository.GetByEmailWithOtpsAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                if (existingUser.EmailVerified)
                    return Result<RegisterResponseDto>.Failure(Errors.Codes.Auth.EmailAlreadyExists, Errors.Messages.Auth.EmailAlreadyExists);

                return await ResendVerificationAsync(existingUser, cancellationToken);
            }

            User user;
            try
            {
                user = User.Create(requestDto.Name, email, _hasher.Hash(requestDto.Password), requestDto.Role);
            }
            catch (ArgumentException ex)
            {
                return Result<RegisterResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var otpResult = await SendEmailVerificationOtpAsync(user, cancellationToken);
            if (otpResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(otpResult.ErrorCode!, otpResult.ErrorMessage!);

            return Result<RegisterResponseDto>.Success(
                new RegisterResponseDto("Registration successful. Please check your email to verify your account."));
        }

        private async Task<Result<RegisterResponseDto>> ResendVerificationAsync(User user, CancellationToken cancellationToken)
        {
            var otpResult = await SendEmailVerificationOtpAsync(user, cancellationToken);
            if (otpResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(otpResult.ErrorCode!, otpResult.ErrorMessage!);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RegisterResponseDto>.Success(
                new RegisterResponseDto("Account already exists but is not verified. A new verification code has been sent."));
        }

        private async Task<Result> SendEmailVerificationOtpAsync(User user, CancellationToken cancellationToken)
        {
            var latestOtp = user.Otps
                .Where(o => o.Purpose == OtpPurpose.EmailVerification)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (latestOtp is not null)
            {
                var secondsSinceLastOtp = (DateTime.UtcNow - latestOtp.CreatedAt).TotalSeconds;
                if (secondsSinceLastOtp < OtpCooldownSeconds)
                {
                    var remaining = OtpCooldownSeconds - (int)secondsSinceLastOtp;
                    return Result.Failure(Errors.Codes.Auth.OtpCooldown, $"Please wait {remaining} seconds before requesting a new verification code.");
                }
            }

            user.InvalidatePreviousOtps(OtpPurpose.EmailVerification);

            var otpCode = _otpService.GenerateOtp(6);
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email.Value,
                    "Verify your email - Roomly",
                    $"<h1>Welcome, {user.Name}!</h1><p>Your verification code is: <b>{otpCode}</b></p><p>This code will expire in {OtpExpirationMinutes} minutes.</p>",
                    cancellationToken);
            }
            catch
            {
                return Result.Failure(Errors.Codes.Auth.EmailSendFailed, Errors.Messages.Auth.EmailSendFailed);
            }

            user.AddOtp(Otp.Create(
                user.Id,
                _hasher.Hash(otpCode),
                OtpPurpose.EmailVerification,
                DateTime.UtcNow.AddMinutes(OtpExpirationMinutes),
                name: "Email Verification",
                description: "OTP sent for email verification"));

            return Result.Success();
        }
    }
}