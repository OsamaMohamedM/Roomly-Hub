using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IGoogleLoginService
    {
        Task<Result<LoginResponseDto>> LoginWithGoogleAsync(GoogleAuthRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
