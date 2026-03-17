using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IKycService
    {
        Task<Result<KycSubmissionResponseDto>> SubmitKycAsync(Guid userId, SubmitKycRequestDto requestDto, CancellationToken cancellationToken = default);

        Task<Result<KycSubmissionResponseDto>> ReviewKycAsync(Guid reviewerId, ReviewKycRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}