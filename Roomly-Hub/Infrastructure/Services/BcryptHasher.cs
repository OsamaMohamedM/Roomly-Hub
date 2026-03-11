using Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{
    public class BCryptHasher : IHasher
    {
        public string Hash(string input)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            return BCrypt.Net.BCrypt.HashPassword(input, salt);
        }

        public bool Verify(string input, string hashedValue)
        {
            return BCrypt.Net.BCrypt.Verify(input, hashedValue);
        }

        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
