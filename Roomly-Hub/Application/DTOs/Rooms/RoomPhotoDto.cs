namespace Application.DTOs
{
    public class RoomPhotoDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}