using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;
        private readonly IHasher _passwordHasher;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<RegisterRequestDto> _registerValidator;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<OtpVerifyDto> _otpVerifyValidator;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            ITokenService tokenService,
            IHasher passwordHasher,
            IOtpService otpService,
            IUnitOfWork unitOfWork,
            IValidator<RegisterRequestDto> registerValidator,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<OtpVerifyDto> otpVerifyValidator,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _otpVerifyValidator = otpVerifyValidator;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto registerRequestDto, CancellationToken cancellationToken = default)
        {
            if (registerRequestDto is null)
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _registerValidator.ValidateAsync(registerRequestDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = ToErrorDictionary(validationResult);
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", errors);
            }

            Email email;
            try
            {
                email = Email.Create(registerRequestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            var existingUser = await _userRepository.GetByEmailWithOtpsAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                if (existingUser.EmailVerified)
                    return Result<RegisterResponseDto>.Failure("EMAIL_ALREADY_EXISTS", "Email is already in use.");

                return await ResendVerificationForUnverifiedUserAsync(existingUser, cancellationToken);
            }

            User user;
            try
            {
                var hashedPassword = _passwordHasher.Hash(registerRequestDto.Password);
                user = User.Create(registerRequestDto.Name, email, hashedPassword, registerRequestDto.Role);
            }
            catch (ArgumentException ex)
            {
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            var otpSendResult = await CreateAndSendEmailVerificationOtpAsync(user, cancellationToken);
            if (otpSendResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(otpSendResult.ErrorCode!, otpSendResult.ErrorMessage!);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RegisterResponseDto>.Success(
                new RegisterResponseDto("Registration successful. Please check your email to verify your account."));
        }

        public async Task<Result<EmailVerifyResponseDto>> VerifyEmailAsync(OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken = default)
        {
            if (otpVerifyDto is null)
                return Result<EmailVerifyResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _otpVerifyValidator.ValidateAsync(otpVerifyDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = ToErrorDictionary(validationResult);
                return Result<EmailVerifyResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", errors);
            }

            var user = await _userRepository.GetByIdWithOtpsAsync(otpVerifyDto.UserId, cancellationToken);
            if (user is null)
                return Result<EmailVerifyResponseDto>.Failure("USER_NOT_FOUND", "User not found.");

            if (user.EmailVerified)
                return Result<EmailVerifyResponseDto>.Failure("EMAIL_ALREADY_VERIFIED", "Email is already verified.");

            var validOtpRecords = user.Otps
                .Where(o => o.Purpose == OtpPurpose.EmailVerification && !o.IsExpired() && !o.IsUsed())
                .ToList();

            if (validOtpRecords.Count == 0)
                return Result<EmailVerifyResponseDto>.Failure("INVALID_OTP", "No valid OTP found for this user.");

            var matchedOtp = validOtpRecords.FirstOrDefault(o => _otpService.VerifyOtp(otpVerifyDto.Otp, o.CodeHash));
            if (matchedOtp is null)
                return Result<EmailVerifyResponseDto>.Failure("INVALID_OTP", "The provided OTP is invalid.");

            matchedOtp.MarkAsUsed();
            user.VerifyEmail();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<EmailVerifyResponseDto>.Success(new EmailVerifyResponseDto("Email verified successfully."));
        }

        public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default)
        {
            if (loginRequestDto is null)
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            var validationResult = await _loginValidator.ValidateAsync(loginRequestDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = ToErrorDictionary(validationResult);
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", errors);
            }

            Email email;
            try
            {
                email = Email.Create(loginRequestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<LoginResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null)
                return Result<LoginResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");

            if (!_passwordHasher.Verify(loginRequestDto.Password, user.PasswordHash))
            {
                if (!user.IsLocked)
                {
                    user.IncrementLoginFailCount();
                    _userRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return Result<LoginResponseDto>.Failure("INVALID_CREDENTIALS", "Invalid email or password.");
            }

            if (user.IsLocked && user.LockoutTokenExpiresAt.HasValue && user.LockoutTokenExpiresAt < DateTime.UtcNow)
            {
                user.Unlock();
                _userRepository.Update(user);
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

            var refreshTokenRecord = RefreshToken.Create(
                user.Id,
                _passwordHasher.Hash(refreshTokenString),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

            user.AddRefreshToken(refreshTokenRecord);
            user.ResetLoginFailCount();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponseDto>.Success(
                new LoginResponseDto(accessToken, refreshTokenString, "Login successful."));
        }

        private async Task<Result<RegisterResponseDto>> ResendVerificationForUnverifiedUserAsync(User user, CancellationToken cancellationToken)
        {
            var otpSendResult = await CreateAndSendEmailVerificationOtpAsync(user, cancellationToken);
            if (otpSendResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(otpSendResult.ErrorCode!, otpSendResult.ErrorMessage!);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RegisterResponseDto>.Success(
                new RegisterResponseDto("Account already exists but is not verified. A new verification code has been sent."));
        }

        private async Task<Result> CreateAndSendEmailVerificationOtpAsync(User user, CancellationToken cancellationToken)
        {
            user.InvalidatePreviousOtps(OtpPurpose.EmailVerification);

            var otpCode = _otpService.GenerateOtp(6);
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email.Value,
                    "Verify your email - Roomly",
                    $"<h1>Welcome, {user.Name}!</h1><p>Your verification code is: <b>{otpCode}</b></p><p>This code will expire in 25 minutes.</p>",
                    cancellationToken);
            }
            catch
            {
                return Result.Failure("EMAIL_SEND_FAILED", "Failed to send verification email.");
            }

            var otpRecord = Otp.Create(
                user.Id,
                _passwordHasher.Hash(otpCode),
                OtpPurpose.EmailVerification,
                DateTime.UtcNow.AddMinutes(25),
                name: "Email Verification",
                description: "OTP sent for email verification");

            user.AddOtp(otpRecord);
            return Result.Success();
        }

        private static Dictionary<string, string[]> ToErrorDictionary(FluentValidation.Results.ValidationResult validationResult)
        {
            return validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        }
    }
}