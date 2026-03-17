namespace Domain.Entities.Room
{
    public class RoomAmenitie : BaseEntity
    {
        public RoomAmenitie()
        { }

        public Guid RoomId { get; set; }
        public Guid AmenityId { get; set; }
        public AmenityType Amenity { get; set; }
    }
}