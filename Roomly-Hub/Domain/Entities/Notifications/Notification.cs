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

        public static Notification Create(Guid userId, NotificationType type, NotificationCategory category, NotificationChannel channel, string title, string body, string? actionUrl)
        {
            return new Notification
            {
                UserId = userId,
                Type = type,
                Category = category,
                Channel = channel,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                Status = NotificationStatus.Unread,
                SentAt = DateTime.UtcNow
            };
        }

        public void MarkAsRead()
        {
            if (Status == NotificationStatus.Read)
                return;

            Status = NotificationStatus.Read;
            ReadAt = DateTime.UtcNow;
            MarkUpdated();
        }

        public bool IsRead => Status == NotificationStatus.Read;
    }
}