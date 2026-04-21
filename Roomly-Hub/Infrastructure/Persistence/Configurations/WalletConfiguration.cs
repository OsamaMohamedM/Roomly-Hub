using Domain.Entities;
using Domain.Entities.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.UserId)
                .IsRequired();

            builder.Property(w => w.Balance)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(w => w.InsuranceHeldBalance)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.HasOne<User>()
                .WithOne()
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(w => w.UserId)
                .IsUnique();
        }
    }
}