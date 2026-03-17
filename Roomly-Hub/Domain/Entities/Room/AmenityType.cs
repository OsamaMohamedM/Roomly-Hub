namespace Domain.Entities.Room
{
    public class AmenityType : BaseEntity
    {
        public AmenityType()
        { }

        public AmenityType(string Name, string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}