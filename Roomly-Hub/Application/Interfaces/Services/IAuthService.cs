using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<EmailVerifyResponseDto>> VerifyEmailAsync(OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken = default);

        Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto loginRequestDto, CancellationToken cancellationToken = default);
    }
}