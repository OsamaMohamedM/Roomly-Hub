namespace Application.DTOs
{
    public class EmailVerifyResponseDto
    {
        public string Message { get; set; }

        public EmailVerifyResponseDto(string message = "Email verified successfully")
        {
            Message = message;
        }
    }
}
