namespace Application.Interfaces.Services
{
    public interface IHasher
    {
        string Hash(string input);
        bool Verify(string input, string hashedValue);
        string HashToken(string token);
    }
}