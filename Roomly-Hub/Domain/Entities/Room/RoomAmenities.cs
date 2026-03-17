namespace Domain.Entities.Room
{
    public class RoomAmenities : BaseEntity
    {
        public RoomAmenities()
        { }

        public Guid RoomId { get; set; }
        public Guid AmenityId { get; set; }
        public AmenityType Amenity { get; set; }
    }
}