using Domain.Entities;
using Domain.enums.Notifications;

namespace Domain.Entities.Notifications
{
    public class NotificationChannelPreference : BaseEntity
    {
        public Guid UserId { get; private set; }
        public NotificationCategory Category { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public bool IsEnabled { get; private set; }

        private NotificationChannelPreference()
        {
        }

        public static NotificationChannelPreference Create(Guid userId, NotificationCategory category, NotificationChannel channel, bool isEnabled)
        {
            return new NotificationChannelPreference
            {
                UserId = userId,
                Category = category,
                Channel = channel,
                IsEnabled = isEnabled
            };
        }

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            MarkUpdated();
        }
    }
}