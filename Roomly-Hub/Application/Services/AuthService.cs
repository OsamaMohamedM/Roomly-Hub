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
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto registerRequestDto)
        {
            if (registerRequestDto is null)
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            if (string.IsNullOrWhiteSpace(registerRequestDto.Email))
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Email is required.");

            if (string.IsNullOrWhiteSpace(registerRequestDto.Password))
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Password is required.");

            if (string.IsNullOrWhiteSpace(registerRequestDto.Name))
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", "Name is required.");

            Email email;
            try
            {
                email = Email.Create(registerRequestDto.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<RegisterResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser is not null)
                return Result<RegisterResponseDto>.Failure("EMAIL_ALREADY_EXISTS", "Email is already in use.");

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

            await _userRepository.AddAsync(user);

            string otpCode;
            try
            {
                otpCode = await _emailService.SendVerificationEmailAsync(user);
            }
            catch
            {
                return Result<RegisterResponseDto>.Failure("EMAIL_SEND_FAILED", "Registration failed while sending verification email.");
            }

            var otpRecord = Otp.Create(
                user.Id,
                _passwordHasher.Hash(otpCode),
                OtpPurpose.EmailVerification,
                DateTime.UtcNow.AddMinutes(25),
                name: "Email Verification",
                description: "OTP sent for email verification");

            user.AddOtp(otpRecord);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result<RegisterResponseDto>.Success(
                new RegisterResponseDto("Registration successful. Please check your email to verify your account."));
        }
    }
}