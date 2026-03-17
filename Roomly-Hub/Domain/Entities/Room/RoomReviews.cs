namespace Domain.Entities.Room
{
    public class RoomReviews : BaseEntity
    {
        public RoomReviews()
        { }

        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; }
        public decimal Rating { get; set; }
    }
}