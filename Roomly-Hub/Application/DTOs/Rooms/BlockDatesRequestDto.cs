namespace Application.DTOs
{
    public class BlockDatesRequestDto
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
    }
}