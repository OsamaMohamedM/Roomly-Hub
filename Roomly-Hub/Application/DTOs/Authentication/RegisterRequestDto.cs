using Domain.Enums;

namespace Application.DTOs
{
    public class RegisterRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }

        public UserRole Role { get; set; }
    }
}