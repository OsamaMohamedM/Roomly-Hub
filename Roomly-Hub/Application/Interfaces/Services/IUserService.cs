using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<Result<UserProfileResponseDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Result<UserProfileResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}
