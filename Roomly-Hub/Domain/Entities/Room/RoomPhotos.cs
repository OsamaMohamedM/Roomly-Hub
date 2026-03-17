namespace Domain.Entities.Room
{
    public class RoomPhotos : BaseEntity
    {
        public RoomPhotos()
        { }

        public Guid RoomId { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }
}