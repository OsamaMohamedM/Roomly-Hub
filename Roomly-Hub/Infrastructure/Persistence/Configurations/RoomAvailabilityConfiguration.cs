using Domain.Entities.Rooms;
using Domain.enums.Room;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class RoomAvailabilityConfiguration : IEntityTypeConfiguration<RoomAvailability>
    {
        public void Configure(EntityTypeBuilder<RoomAvailability> builder)
        {
            builder.ToTable("RoomAvailabilities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RoomId)
                .IsRequired();

            builder.Property(x => x.BlockedDate)
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasConversion<string>()
                .HasColumnType("varchar(40)")
                .HasDefaultValue(AvailabilityReason.HostBlocked)
                .IsRequired();

            builder.HasIndex(x => new { x.RoomId, x.BlockedDate })
                .IsUnique();
        }
    }
}
