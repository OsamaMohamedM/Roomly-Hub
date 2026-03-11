namespace Application.DTOs
{
    public class OtpVerifyDto
    {
        public string Otp { get; set; } = string.Empty;
        public Guid UserId { get; set; }

        public OtpVerifyDto()
        { }

        public OtpVerifyDto(string otp, Guid userId)
        {
            Otp = otp;
            UserId = userId;
        }
    }
}