using Domain.enums.Room;

namespace Domain.Entities.Rooms
{
    public class RoomAvailability : BaseEntity
    {
        public Guid RoomId { get; private set; }
        public DateOnly BlockedDate { get; private set; }
        public AvailabilityReason Reason { get; private set; }

        private RoomAvailability()
        {
        }

        public static RoomAvailability Create(Guid roomId, DateOnly blockedDate, AvailabilityReason reason)
        {
            if (roomId == Guid.Empty)
                throw new ArgumentException("Room ID is required.", nameof(roomId));

            if (reason == AvailabilityReason.None)
                throw new ArgumentException("Availability reason is required.", nameof(reason));

            return new RoomAvailability
            {
                RoomId = roomId,
                BlockedDate = blockedDate,
                Reason = reason
            };
        }
    }
}
