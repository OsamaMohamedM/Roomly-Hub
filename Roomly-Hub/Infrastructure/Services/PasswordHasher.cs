using Application.Interfaces.Services;

namespace Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            return BCrypt.Net.BCrypt.HashPassword(password, salt);
        }

        public bool Verify(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}