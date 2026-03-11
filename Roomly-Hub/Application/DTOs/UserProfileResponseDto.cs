using Domain.Enums;

namespace Application.DTOs
{
    public class UserProfileResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? Bio { get; set; }
        public bool EmailVerified { get; set; }
        public SubmissionStatus KycStatus { get; set; }
        public int KycAttemptCount { get; set; }
        public UserRole Role { get; set; }
        public AdminRole AdminRole { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
