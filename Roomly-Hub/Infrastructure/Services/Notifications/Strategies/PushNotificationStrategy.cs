using Application.Common.Results;
using Application.Interfaces.Strategies;
using Domain.enums.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Services.Notifications.Strategies
{
    public class PushNotificationStrategy : INotificationChannelStrategy
    {
        private readonly ILogger<PushNotificationStrategy> _logger;

        public PushNotificationStrategy(ILogger<PushNotificationStrategy> logger)
        {
            _logger = logger;
        }

        public NotificationChannel ChannelType => NotificationChannel.Push;

        public Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken ct)
        {
            _logger.LogInformation("Push notification stub for user {UserId}", userId);
            return Task.FromResult(Result.Success());
        }
    }
}
