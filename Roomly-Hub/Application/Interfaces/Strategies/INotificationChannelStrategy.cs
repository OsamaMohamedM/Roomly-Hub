using Application.Common.Results;
using Domain.enums.Notifications;

namespace Application.Interfaces.Strategies
{
    public interface INotificationChannelStrategy
    {
        NotificationChannel ChannelType { get; }

        Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken ct);
    }
}