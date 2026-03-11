using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class OtpConfig : IEntityTypeConfiguration<Otp>
    {
        public void Configure(EntityTypeBuilder<Otp> builder)
        {
            builder.ToTable("Otps");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(o => o.Description)
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(o => o.CodeHash)
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(o => o.UserId)
                .IsRequired();
            builder.Property(o => o.Purpose)
                .IsRequired();
            builder.Property(o => o.ExpiresAt)
                .IsRequired();
        }
    }
}