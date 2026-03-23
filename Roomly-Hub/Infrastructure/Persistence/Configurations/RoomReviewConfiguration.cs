using Domain.Entities.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class RoomReviewConfiguration : IEntityTypeConfiguration<RoomReview>
    {
        public void Configure(EntityTypeBuilder<RoomReview> builder)
        {
            builder.ToTable("RoomReviews", table =>
            {
                table.HasCheckConstraint("CK_RoomReviews_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
            });

            builder.HasKey(r => r.Id);

            builder.Property(r => r.RoomId)
                .IsRequired();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.Comment)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(r => r.Rating)
                .IsRequired()
                .HasColumnType("decimal(3,2)");

            builder.HasOne<Room>()
                .WithMany()
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.RoomId);
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => new { r.RoomId, r.UserId })
                .IsUnique();
        }
    }
}