using Domain.Entities;
using Domain.Entities.Notifications;
using Domain.enums.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class NotificationChannelPreferenceConfiguration : IEntityTypeConfiguration<NotificationChannelPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationChannelPreference> builder)
        {
            builder.ToTable("NotificationChannelPreferences");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Category).IsRequired().HasConversion<string>().HasColumnType("varchar(50)");
            builder.Property(x => x.Channel).IsRequired().HasConversion<string>().HasColumnType("varchar(20)");
            builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.Category, x.Channel }).IsUnique();
        }
    }
}