using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IRefreshTokenService
    {
        Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
