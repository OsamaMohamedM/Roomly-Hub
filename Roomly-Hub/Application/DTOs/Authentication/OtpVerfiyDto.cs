namespace Application.DTOs
{
    public class OtpVerifyDto
    {
        public string Otp { get; set; } = string.Empty;
        public string Email { get; set; }

        public OtpVerifyDto()
        { }

        public OtpVerifyDto(string otp, string email)
        {
            Otp = otp;
            Email = email;
        }
    }
}