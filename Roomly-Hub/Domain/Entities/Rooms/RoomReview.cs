namespace Domain.Entities.Rooms
{
    public class RoomReview : BaseEntity
    {
        public RoomReview()
        { }

        public Guid RoomId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; }
        public decimal Rating { get; set; }
    }
}