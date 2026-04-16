using Domain.enums.Reviews;

namespace Domain.Entities.Reviews
{
    public class Review : BaseEntity
    {
        public Guid ReviewerId { get; private set; }
        public Guid? SubjectUserId { get; private set; }
        public Guid? SubjectRoomId { get; private set; }
        public Guid BookingId { get; private set; }
        public int Rating { get; private set; }
        public string Comment { get; private set; } = string.Empty;
        public string? FlagReason { get; private set; }
        public ReviewType Type { get; private set; }
        public ReviewStatus Status { get; private set; } = ReviewStatus.Visible;

        private Review(Guid reviewerId, Guid? subjectUserId, Guid? subjectRoomId, Guid bookingId, int rating, string comment, ReviewType type)
        {
            ReviewerId = reviewerId;
            SubjectUserId = subjectUserId;
            SubjectRoomId = subjectRoomId;
            BookingId = bookingId;
            Rating = rating;
            Comment = comment;
            Type = type;
        }

        public static Review Create(Guid reviewerId, Guid? subjectUserId, Guid? subjectRoomId, Guid bookingId, int rating, string comment, ReviewType type)
        {
            if (rating < 1 || rating > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
            }
            if (subjectRoomId == null)
            {
                throw new ArgumentException("SubjectRoomId must be provided for room reviews.", nameof(subjectRoomId));
            }
            if (subjectUserId == null)
            {
                throw new ArgumentException("SubjectUserId must be provided for user reviews.", nameof(subjectUserId));
            }
            return new Review(reviewerId, subjectUserId, subjectRoomId, bookingId, rating, comment, type);
        }

        public void Flag(string reason)
        {
            Status = ReviewStatus.Flagged;
            FlagReason = reason;
        }

        public bool IsFlagged()
        {
            return Status == ReviewStatus.Flagged;
        }

        public void Remove()
        {
            Status = ReviewStatus.Removed;
        }

        public void MakeVisible()
        {
            Status = ReviewStatus.Visible;
            FlagReason = null;
        }

        public void UpdateComment(string newComment)
        {
            Comment = newComment;
        }

        public bool IsVisible()
        {
            return Status == ReviewStatus.Visible;
        }
    }
}