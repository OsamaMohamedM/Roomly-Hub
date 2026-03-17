namespace Application.DTOs
{
    public class UpdateProfileRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
}
