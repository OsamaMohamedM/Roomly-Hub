using Domain.enums.Auction;

namespace Domain.Entities.Auctions
{
    public class AuctionBid : BaseEntity
    {
        private AuctionBid()
        {
        }

        public Guid AuctionId { get; private set; }
        public Guid BidderId { get; private set; }
        public decimal Amount { get; private set; }
        public bool IsWinning { get; private set; }
        public BidStatus Status { get; private set; }
        public bool InsuranceLocked { get; private set; }
        public DateTime PlacedAt { get; private set; }

        public static AuctionBid Create(Guid auctionId, Guid bidderId, decimal amount)
        {
            if (auctionId == Guid.Empty)
                throw new ArgumentException("AuctionId is required.", nameof(auctionId));

            if (bidderId == Guid.Empty)
                throw new ArgumentException("BidderId is required.", nameof(bidderId));

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than 0.", nameof(amount));

            return new AuctionBid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                Amount = amount,
                IsWinning = true,
                Status = BidStatus.Winning,
                InsuranceLocked = false,
                PlacedAt = DateTime.UtcNow
            };
        }

        public void MarkOutbid()
        {
            if (Status != BidStatus.Winning)
                throw new InvalidOperationException("Only winning bid can be outbid.");

            IsWinning = false;
            Status = BidStatus.Outbid;
            MarkUpdated();
        }

        public void MarkInsuranceLocked()
        {
            if (InsuranceLocked)
                throw new InvalidOperationException("Insurance is already locked.");

            InsuranceLocked = true;
            MarkUpdated();
        }

        public void MarkForfeited()
        {
            if (Status != BidStatus.Winning)
                throw new InvalidOperationException("Only winning bid can be forfeited.");

            Status = BidStatus.Forfeited;
            IsWinning = false;
            MarkUpdated();
        }

        public void MarkPaid()
        {
            if (Status != BidStatus.Winning)
                throw new InvalidOperationException("Only winning bid can be paid.");

            Status = BidStatus.Paid;
            MarkUpdated();
        }
    }
}
