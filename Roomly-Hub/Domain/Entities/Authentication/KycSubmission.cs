using Domain.Enums;

namespace Domain.Entities
{
    public class KycSubmission : BaseEntity
    {
        public Guid UserId { get; private set; }
        public KycDocumentType DocumentType { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string FrontImageUrl { get; private set; }
        public string? BackImageUrl { get; private set; }
        public string SelfieUrl { get; private set; }
        public SubmissionStatus Status { get; private set; }
        public string? RejectionReason { get; private set; }
        public Guid? ReviewedBy { get; private set; }
        public DateTime SubmittedAt { get; private set; }
        public DateTime? ReviewedAt { get; private set; }

        private KycSubmission()
        { }

        public static KycSubmission Create(
            Guid userId,
            KycDocumentType documentType,
            string title,
            string description,
            string frontImageUrl,
            string selfieUrl,
            string? backImageUrl = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.", nameof(title));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required.", nameof(description));

            if (string.IsNullOrWhiteSpace(frontImageUrl))
                throw new ArgumentException("Front image URL is required.", nameof(frontImageUrl));

            if (string.IsNullOrWhiteSpace(selfieUrl))
                throw new ArgumentException("Selfie URL is required.", nameof(selfieUrl));

            if (documentType == KycDocumentType.Passport && string.IsNullOrWhiteSpace(backImageUrl))
                throw new ArgumentException("Back image is required for passport.", nameof(backImageUrl));

            return new KycSubmission
            {
                UserId = userId,
                DocumentType = documentType,
                Title = title.Trim(),
                Description = description.Trim(),
                FrontImageUrl = frontImageUrl.Trim(),
                BackImageUrl = backImageUrl?.Trim(),
                SelfieUrl = selfieUrl.Trim(),
                Status = SubmissionStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };
        }

        public void Approve(Guid reviewerId)
        {
            if (reviewerId == Guid.Empty)
                throw new ArgumentException("Reviewer ID is required.", nameof(reviewerId));

            if (Status != SubmissionStatus.Pending)
                throw new InvalidOperationException("Only pending submissions can be approved.");

            Status = SubmissionStatus.Approved;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            RejectionReason = null;
            MarkUpdated();
        }

        public void Reject(Guid reviewerId, string reason)
        {
            if (reviewerId == Guid.Empty)
                throw new ArgumentException("Reviewer ID is required.", nameof(reviewerId));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Rejection reason is required.", nameof(reason));

            if (Status != SubmissionStatus.Pending)
                throw new InvalidOperationException("Only pending submissions can be rejected.");

            if (reason.Length > 500)
                throw new ArgumentException("Rejection reason cannot exceed 500 characters.", nameof(reason));

            Status = SubmissionStatus.Rejected;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            RejectionReason = reason.Trim();
            MarkUpdated();
        }

        public bool IsPending()
        {
            return Status == SubmissionStatus.Pending;
        }

        public bool IsApproved()
        {
            return Status == SubmissionStatus.Approved;
        }

        public bool IsRejected()
        {
            return Status == SubmissionStatus.Rejected;
        }
    }
}