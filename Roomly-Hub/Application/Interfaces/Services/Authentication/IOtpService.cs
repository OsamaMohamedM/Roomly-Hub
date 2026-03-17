namespace Application.Interfaces.Services
{
    public interface IOtpService
    {
        string GenerateOtp(int length = 6);
        bool VerifyOtp(string input, string hashedOtp);
    }
}