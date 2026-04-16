namespace Application.DTOs.Reviews
{
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }

        public string ReviewType { get; set; }

        public string ReviewerName { get; set; }
        public int Rating { get; set; }
        public DateTime Created { get; set; }
        public string? Comment { get; set; }
        public string Status { get; set; }
    }
}