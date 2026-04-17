using Domain.Entities;
using Domain.Entities.Notifications;
using Domain.enums.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Body)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(50)");

            builder.Property(x => x.Category)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(50)");

            builder.Property(x => x.Channel)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(20)");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(20)")
                .HasDefaultValue(NotificationStatus.Unread);

            builder.Property(x => x.ActionUrl)
                .HasMaxLength(500);

            builder.Property(x => x.SentAt)
                .HasDefaultValueSql("NOW()");

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.Status });
            builder.HasIndex(x => new { x.UserId, x.Category });
            builder.HasIndex(x => new { x.UserId, x.Channel });
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}