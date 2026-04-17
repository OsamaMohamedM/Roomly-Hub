using Domain.enums.Notifications;

namespace Application.DTOs.Notifications
{
    public class UpdatePreferenceRequestDto
    {
        public NotificationCategory Category { get; set; }
        public NotificationChannel Channel { get; set; }
        public bool IsEnabled { get; set; }
    }
}