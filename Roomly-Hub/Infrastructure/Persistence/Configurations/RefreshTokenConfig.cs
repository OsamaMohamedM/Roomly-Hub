using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
          
            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(255);
            builder.Property(rt => rt.UserId)
                .IsRequired();
            builder.Property(rt => rt.ExpiresAt)
                .IsRequired();
            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique();
            var foreignKey = builder.HasOne<User>()
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(rt => rt.UserId)
                 .OnDelete(DeleteBehavior.Cascade)
                 .Metadata;
            foreignKey.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}