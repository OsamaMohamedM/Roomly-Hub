using Application.Common.Results;
using Application.DTOs.Notifications;

namespace Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<int>> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> UpdatePreferencesAsync(Guid userId, UpdatePreferenceRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}