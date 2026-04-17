using Domain.Entities;
using Domain.enums.Notifications;

namespace Domain.Entities.Notifications
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Title { get; private set; }
        public string Body { get; private set; }
        public NotificationType Type { get; private set; }
        public NotificationCategory Category { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public NotificationStatus Status { get; private set; }
        public string? ActionUrl { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public DateTime? SentAt { get; private set; }

        private Notification()
        {
            Title = string.Empty;
            Body = string.Empty;
        }
    }
}