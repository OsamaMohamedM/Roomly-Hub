namespace Domain.Entities.Room
{
    public class RoomPhoto : BaseEntity
    {
        public RoomPhoto()
        { }

        public Guid RoomId { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }
}