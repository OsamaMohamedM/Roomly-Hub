using Domain.enums.Notifications;

namespace Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public NotificationType Type { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationChannel Channel { get; set; }
        public NotificationStatus Status { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}