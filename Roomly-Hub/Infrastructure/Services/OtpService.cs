using Application.Interfaces.Services;
using System.Security.Cryptography;

namespace Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly IHasher _hasher;

        public OtpService(IHasher hasher)
        {
            _hasher = hasher;
        }

        public string GenerateOtp(int length = 6)
        {
            var max = (int)Math.Pow(10, length);
            return RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");
        }

        public bool VerifyOtp(string input, string hashedOtp)
            => _hasher.Verify(input, hashedOtp);
    }
}