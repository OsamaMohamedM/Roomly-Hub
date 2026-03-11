using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasConversion(
                    email => email.Value,
                    value => Email.Create(value));

            builder.Property(u => u.PasswordHash)
                .HasMaxLength(255);

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .HasConversion(
                    phone => phone != null ? phone.Value : null,
                    value => value != null ? PhoneNumber.Create(value) : null);

            builder.Property(u => u.ProfilePhotoUrl)
                .HasMaxLength(500);

            builder.Property(u => u.Bio)
                .HasMaxLength(1000);

            builder.Property(u => u.KycStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(20)");

            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(10)");

            builder.Property(u => u.AdminRole)
                .HasConversion<string>()
                .HasColumnType("varchar(20)");

            builder.Property(u => u.LockoutToken)
                .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                .IsUnique();
            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique();
            builder.HasIndex(u => u.LockoutToken)
                .IsUnique();

            // Relationships — all use backing-field collections
            builder.HasMany(u => u.RefreshTokens)
                .WithOne()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ExternalLogins)
                .WithOne()
                .HasForeignKey(el => el.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.KycSubmissions)
                .WithOne()
                .HasForeignKey(ks => ks.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Otps)
                .WithOne()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(u => u.RefreshTokens)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(u => u.ExternalLogins)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(u => u.KycSubmissions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(u => u.Otps)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}