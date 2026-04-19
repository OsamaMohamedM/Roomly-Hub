using Application.Common.Results;
using Application.Interfaces.Persistence;
using Application.Interfaces.Strategies;
using Domain.Entities.Notifications;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;

namespace Application.Services.Notifications.Strategies
{
    public class InAppNotificationStrategy : INotificationChannelStrategy
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InAppNotificationStrategy(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public NotificationChannel ChannelType => NotificationChannel.InApp;

        public async Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken ct)
        {
            var category = NotificationService.ResolveCategory(type);
            var notification = Notification.Create(userId, type, category, NotificationChannel.InApp, title, body, deepLink);
            await _notificationRepository.AddAsync(notification, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
