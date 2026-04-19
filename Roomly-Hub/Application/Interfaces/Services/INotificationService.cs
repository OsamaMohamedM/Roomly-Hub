using Application.Common.Results;
using Application.DTOs.Notifications;
using Domain.enums.Notifications;

namespace Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Result> CreateDefaultPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken cancellationToken = default);

        Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<int>> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> UpdatePreferencesAsync(Guid userId, UpdatePreferenceRequestDto requestDto, CancellationToken cancellationToken = default);
    }
}