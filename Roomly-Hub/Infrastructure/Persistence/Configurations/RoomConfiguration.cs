using Domain.Entities.Room;
using Domain.enums.Room;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("Rooms", table =>
            {
                table.HasCheckConstraint("CK_Rooms_PricePerNight_Positive", "\"PricePerNight\" > 0");
                table.HasCheckConstraint("CK_Rooms_MaxGuests_Positive", "\"MaxGuests\" > 0");
                table.HasCheckConstraint("CK_Rooms_CheckOutTime_After_CheckInTime", "\"CheckOutTime\" > \"CheckInTime\"");
                table.HasCheckConstraint("CK_Rooms_AverageRating_Range", "\"AverageRating\" IS NULL OR (\"AverageRating\" >= 1 AND \"AverageRating\" <= 5)");
            });

            builder.HasKey(r => r.Id);

            builder.Property(r => r.HostId)
                .IsRequired();

            builder.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(r => r.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(r => r.PricePerNight)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(r => r.MaxGuests)
                .IsRequired();

            builder.Property(r => r.RoomType)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(30)");

            builder.Property(r => r.CheckInTime)
                .IsRequired();

            builder.Property(r => r.CheckOutTime)
                .IsRequired();

            builder.Property(r => r.FreeCancellation)
                .IsRequired();

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasDefaultValue(RoomListingStatus.Draft);

            builder.Property(r => r.StatusAvailability)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasDefaultValue(RoomAvailabilityStatus.Available);

            builder.Property(r => r.AverageRating)
                .HasColumnType("decimal(3,2)");

            builder.OwnsOne(r => r.Address, address =>
            {
                address.Property(a => a.Street)
                    .IsRequired()
                    .HasColumnName("AddressLine")
                    .HasMaxLength(300);

                address.Property(a => a.City)
                    .IsRequired()
                    .HasColumnName("City")
                    .HasMaxLength(100);

                address.Property(a => a.State)
                    .IsRequired()
                    .HasColumnName("State")
                    .HasMaxLength(100);

                address.Property(a => a.Country)
                    .IsRequired()
                    .HasColumnName("Country")
                    .HasMaxLength(100);

                address.Property(a => a.ZipCode)
                    .IsRequired()
                    .HasColumnName("ZipCode")
                    .HasMaxLength(20);

                address.Property(a => a.HouseNumber)
                    .IsRequired()
                    .HasColumnName("HouseNumber")
                    .HasMaxLength(20);

                address.Property(a => a.RoomNumber)
                    .IsRequired()
                    .HasColumnName("RoomNumber")
                    .HasMaxLength(20);

                address.HasIndex(a => a.City);
            });

            builder.Navigation(r => r.Address)
                .IsRequired();

            builder.HasMany(r => r.Photos)
                .WithOne()
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Amenities)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "RoomAmenities",
                    right => right.HasOne<AmenityType>()
                        .WithMany()
                        .HasForeignKey("AmenityId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Room>()
                        .WithMany()
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("RoomAmenities");
                        join.HasKey("RoomId", "AmenityId");
                        join.HasIndex("AmenityId");
                    });

            builder.Navigation(r => r.Photos)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Navigation(r => r.Amenities)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(r => r.HostId);
            builder.HasIndex(r => r.RoomType);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => r.StatusAvailability);
            builder.HasIndex(r => r.PricePerNight);
            builder.HasIndex(r => r.AverageRating);
        }
    }
}