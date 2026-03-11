namespace Application.DTOs
{
    public class ReviewKycRequestDto
    {
        public Guid SubmissionId { get; set; }
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
