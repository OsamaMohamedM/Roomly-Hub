using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class UserExternalLoginConfig : IEntityTypeConfiguration<UserExternalLogin>
    {
        public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
        {
            builder.ToTable("UserExternalLogins");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId)
                .IsRequired();

            builder.Property(e => e.Provider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.ExternalId)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.Email)
                .HasMaxLength(255)
                .HasConversion(
                    email => email != null ? email.Value : null,
                    value => value != null ? Email.Create(value) : null);

            builder.HasIndex(e => new { e.Provider, e.ExternalId })
                .IsUnique();

            builder.HasIndex(e => e.UserId);
        }
    }
}