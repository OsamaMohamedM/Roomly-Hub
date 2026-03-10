using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto requestDto);
    }
}