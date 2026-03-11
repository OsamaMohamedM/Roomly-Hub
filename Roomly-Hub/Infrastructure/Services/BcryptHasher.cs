using Application.Interfaces.Services;

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
    }
}
