using Domain.Entities.Booking;
using Domain.enums.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.RoomId).IsRequired();
            builder.Property(b => b.GuestId).IsRequired();
            builder.Property(b => b.CheckInDate).IsRequired();
            builder.Property(b => b.CheckOutDate).IsRequired();
            builder.Property(b => b.CreatedDate).IsRequired();
            builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasDefaultValue(BookingStatus.Pending)
                .IsRequired();

            builder.Property(b => b.Source)
                .HasConversion<string>()
                .HasDefaultValue(SourceStatus.Standard)
                .IsRequired();

            builder.Property(b => b.BookingMode)
                .HasConversion<string>()
                .HasDefaultValue(BookingMode.InstantBook)
                .IsRequired();

            builder.Property(b => b.CancellationPolicy)
                .HasConversion<string>()
                .HasDefaultValue(CancellationPolicy.FreeCancellation)
                .IsRequired();

            builder.Property(b => b.PaymentMethod)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(b => b.PaymentStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(b => b.CancelledAt);
            builder.Property(b => b.CancelledBy);

            builder.HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => b.RoomId);
            builder.HasIndex(b => b.GuestId);
            builder.HasIndex(b => new { b.RoomId, b.CheckInDate, b.CheckOutDate });
        }
    }
}