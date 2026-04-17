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
    }
}