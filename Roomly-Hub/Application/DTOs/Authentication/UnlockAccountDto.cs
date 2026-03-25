namespace Application.DTOs
{
    public class UnlockAccountDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
