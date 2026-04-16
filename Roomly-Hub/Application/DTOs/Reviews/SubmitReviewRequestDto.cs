namespace Application.DTOs.Reviews
{
    public class SubmitReviewRequestDto
    {
        public Guid BookingId { get; set; }
        public string ReviewType { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? SubjectRoomId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}