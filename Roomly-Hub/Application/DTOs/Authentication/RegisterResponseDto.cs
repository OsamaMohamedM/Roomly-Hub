namespace Application.DTOs
{
    public class RegisterResponseDto
    {
        public string Message { get; set; }

        public RegisterResponseDto(string message)
        {
            Message = message;
        }
    }
}