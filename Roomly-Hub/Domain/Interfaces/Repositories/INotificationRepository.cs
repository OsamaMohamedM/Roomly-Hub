using Domain.Entities.Notifications;
using Domain.enums.Notifications;

namespace Domain.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddPreferenceAsync(NotificationChannelPreference preference, CancellationToken cancellationToken = default);

        Task<NotificationChannelPreference?> GetPreferenceAsync(Guid userId, NotificationCategory category, NotificationChannel channel, CancellationToken cancellationToken = default);

        Task UpdatePreferenceAsync(NotificationChannelPreference preference, CancellationToken cancellationToken = default);
    }
}