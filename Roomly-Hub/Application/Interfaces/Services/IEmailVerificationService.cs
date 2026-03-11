using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IEmailVerificationService
    {
        Task<Result<EmailVerifyResponseDto>> VerifyEmailAsync(OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken = default);
    }
}
