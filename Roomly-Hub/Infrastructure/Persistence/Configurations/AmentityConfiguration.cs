using Domain.Entities.Room;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class AmentityConfiguration : IEntityTypeConfiguration<AmenityType>
    {
        public void Configure(EntityTypeBuilder<AmenityType> builder)
        {
            builder.ToTable("Amenities");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(a => a.Name)
                .IsUnique();
        }
    }
}