using Domain.Entities.Room;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class RoomPhotoConfiguration : IEntityTypeConfiguration<RoomPhoto>
    {
        public void Configure(EntityTypeBuilder<RoomPhoto> builder)
        {
            builder.ToTable("RoomPhotos");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.RoomId)
                .IsRequired();

            builder.Property(p => p.Url)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.HasIndex(p => p.RoomId);
            builder.HasIndex(p => new { p.RoomId, p.Url })
                .IsUnique();
        }
    }
}