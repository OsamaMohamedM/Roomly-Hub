using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IRegisterService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
