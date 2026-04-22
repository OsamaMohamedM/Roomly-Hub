using Domain.Entities;
using Domain.Entities.Auctions;
using Domain.Entities.Rooms;
using Domain.enums.Auction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class AuctionConfiguration : IEntityTypeConfiguration<Auction>
    {
        public void Configure(EntityTypeBuilder<Auction> builder)
        {
            builder.ToTable("Auctions");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.StartingPrice).HasPrecision(18, 4).IsRequired();
            builder.Property(a => a.CurrentHighestBid).HasPrecision(18, 4);
            builder.Property(a => a.MinBidIncrementValue).HasPrecision(18, 4).IsRequired();
            builder.Property(a => a.InsuranceDepositRate).HasPrecision(18, 4).IsRequired();
            builder.Property(a => a.InsuranceDepositAmount).HasPrecision(18, 4).IsRequired();

            builder.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(a => a.MinBidIncrementType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(a => a.Duration)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne<Room>()
                .WithMany()
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.HostId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(a => a.Bids)
                .WithOne()
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.RoomId, a.Status });
            builder.HasIndex(a => new { a.HostId, a.Status });
            builder.HasIndex(a => new { a.Status, a.EndTime });
        }
    }
}