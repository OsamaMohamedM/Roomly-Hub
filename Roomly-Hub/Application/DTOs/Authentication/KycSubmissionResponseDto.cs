using Domain.Enums;

namespace Application.DTOs
{
    public class KycSubmissionResponseDto
    {
        public Guid Id { get; set; }
        public SubmissionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
