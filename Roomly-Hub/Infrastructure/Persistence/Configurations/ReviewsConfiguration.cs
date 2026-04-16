using Domain.Entities.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Configurations
{
    internal class ReviewsConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReviewerId).IsRequired();
            builder.Property(r => r.BookingId).IsRequired();
            builder.Property(r => r.Rating).IsRequired();
            builder.Property(r => r.Comment).HasMaxLength(1000);
            builder.Property(r => r.FlagReason).HasMaxLength(500);
            builder.Property(r => r.Type).IsRequired();
            builder.Property(r => r.Status).IsRequired();
            builder.HasIndex(r => r.BookingId).IsUnique();
            builder.HasIndex(r => r.SubjectUserId);
            builder.HasIndex(r => r.SubjectRoomId);
        }
    }
}