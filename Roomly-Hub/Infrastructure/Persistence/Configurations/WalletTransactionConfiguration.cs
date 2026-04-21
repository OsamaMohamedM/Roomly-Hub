using Domain.Entities.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.WalletId)
                .IsRequired();

            builder.Property(t => t.Amount)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.ReferenceType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(t => t.ReferenceId);

            builder.Property(t => t.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(t => t.ExternalRef)
                .HasMaxLength(255);

            builder.Property(t => t.Description)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(t => t.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasOne<Wallet>()
                .WithMany()
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            builder.HasIndex(t => new { t.WalletId, t.CreatedAt });
        }
    }
}