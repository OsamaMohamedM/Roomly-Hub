using Domain.Entities;
using Domain.Entities.Auctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class AuctionBidConfiguration : IEntityTypeConfiguration<AuctionBid>
    {
        public void Configure(EntityTypeBuilder<AuctionBid> builder)
        {
            builder.ToTable("AuctionBids");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Amount)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(15)
                .IsRequired();

            builder.HasOne<Auction>()
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.BidderId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(b => new { b.AuctionId, b.Amount });
            builder.HasIndex(b => b.BidderId);
        }
    }
}
