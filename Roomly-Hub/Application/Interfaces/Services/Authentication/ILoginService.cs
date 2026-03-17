using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface ILoginService
    {
        Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
