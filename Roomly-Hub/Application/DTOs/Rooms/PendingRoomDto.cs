using Domain.Entities.Rooms;

namespace Application.DTOs.Rooms
{
    public class PendingRoomDto
    {
        public Guid Id { get; set; }
        public Guid HostId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public Room Room { get; set; }
    }
}