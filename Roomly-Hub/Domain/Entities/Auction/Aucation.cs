using Domain.Constants;
using Domain.enums.Auction;

namespace Domain.Entities.Auctions
{
    public class Auction : BaseEntity
    {
        private Auction()
        {
        }

        public Guid RoomId { get; private set; }
        public Guid HostId { get; private set; }
        public DateOnly CheckInDate { get; private set; }
        public DateOnly CheckOutDate { get; private set; }
        public decimal StartingPrice { get; private set; }
        public decimal? CurrentHighestBid { get; private set; }
        public Guid? WinnerId { get; private set; }
        public IncrementType MinBidIncrementType { get; private set; }
        public decimal MinBidIncrementValue { get; private set; }
        public decimal InsuranceDepositRate { get; private set; }
        public decimal InsuranceDepositAmount { get; private set; }
        public AuctionDuration Duration { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public AuctionStatus Status { get; private set; }
        public DateTime? PaymentDeadline { get; private set; }
        public int CurrentCascadeDepth { get; private set; }
        public string? SettlementJobId { get; private set; }
        public string? EndingSoonJobId { get; private set; }
        public ICollection<AuctionBid> Bids { get; private set; } = new List<AuctionBid>();

        public static Auction Create(
            Guid roomId,
            Guid hostId,
            DateOnly checkIn,
            DateOnly checkOut,
            decimal startingPrice,
            IncrementType minBidIncrType,
            decimal minBidIncrValue,
            decimal insuranceRate,
            AuctionDuration duration,
            DateTime startTime,
            DateTime endTime)
        {
            if (roomId == Guid.Empty)
                throw new ArgumentException("RoomId is required.", nameof(roomId));

            if (hostId == Guid.Empty)
                throw new ArgumentException("HostId is required.", nameof(hostId));

            if (checkOut <= checkIn)
                throw new ArgumentException("CheckOutDate must be after CheckInDate.");

            if (startingPrice <= 0)
                throw new ArgumentException("StartingPrice must be greater than 0.", nameof(startingPrice));

            if (minBidIncrValue <= 0)
                throw new ArgumentException("MinBidIncrementValue must be greater than 0.", nameof(minBidIncrValue));

            if (insuranceRate <= 0)
                throw new ArgumentException("InsuranceDepositRate must be greater than 0.", nameof(insuranceRate));

            if (startTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("StartTime must be UTC.", nameof(startTime));

            if (endTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("EndTime must be UTC.", nameof(endTime));

            var checkInDateTimeUtc = DateTime.SpecifyKind(checkIn.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            if (endTime > checkInDateTimeUtc.AddHours(-AuctionConstants.EndTimePriorCheckInHours))
                throw new ArgumentException("EndTime must be at least 24 hours before check-in.", nameof(endTime));

            if (endTime <= startTime)
                throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

            return new Auction
            {
                RoomId = roomId,
                HostId = hostId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                StartingPrice = startingPrice,
                CurrentHighestBid = null,
                WinnerId = null,
                MinBidIncrementType = minBidIncrType,
                MinBidIncrementValue = minBidIncrValue,
                InsuranceDepositRate = insuranceRate,
                InsuranceDepositAmount = startingPrice * insuranceRate,
                Duration = duration,
                StartTime = startTime,
                EndTime = endTime,
                Status = AuctionStatus.Active,
                PaymentDeadline = null,
                CurrentCascadeDepth = 0,
                SettlementJobId = null,
                EndingSoonJobId = null
            };
        }

        public void ExtendEndTime(int seconds)
        {
            if (Status != AuctionStatus.Active)
                throw new InvalidOperationException("Only active auction can be extended.");

            EndTime = EndTime.AddSeconds(seconds);
            MarkUpdated();
        }

        public void UpdateHighestBid(decimal amount, Guid bidderId)
        {
            if (Status != AuctionStatus.Active)
                throw new InvalidOperationException("Only active auction can accept bids.");

            var current = CurrentHighestBid ?? 0m;
            if (amount <= current)
                throw new ArgumentException("Amount must be greater than current highest bid.", nameof(amount));

            CurrentHighestBid = amount;
            WinnerId = bidderId;
            MarkUpdated();
        }

        public void SetPendingPayment(Guid winnerId, DateTime deadline)
        {
            if (Status != AuctionStatus.Active)
                throw new InvalidOperationException("Only active auction can move to pending payment.");

            if (winnerId == Guid.Empty)
                throw new ArgumentException("WinnerId is required.", nameof(winnerId));

            if (deadline.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Deadline must be UTC.", nameof(deadline));

            Status = AuctionStatus.Pending_Payment;
            WinnerId = winnerId;
            PaymentDeadline = deadline;
            MarkUpdated();
        }

        public void Complete()
        {
            if (Status != AuctionStatus.Pending_Payment)
                throw new InvalidOperationException("Only pending payment auction can be completed.");

            Status = AuctionStatus.Completed;
            MarkUpdated();
        }

        public void ExpireNoBids()
        {
            if (Status != AuctionStatus.Active || CurrentHighestBid.HasValue)
                throw new InvalidOperationException("Only active auction with no bids can be expired as no-bids.");

            Status = AuctionStatus.Expired_NoBids;
            MarkUpdated();
        }

        public void CancelByHost()
        {
            if (Status != AuctionStatus.Active)
                throw new InvalidOperationException("Only active auction can be cancelled by host.");

            Status = AuctionStatus.Cancelled_By_Host;
            MarkUpdated();
        }

        public void CascadeToNext(Guid nextWinnerId, DateTime newDeadline)
        {
            if (Status != AuctionStatus.Pending_Payment)
                throw new InvalidOperationException("Only pending payment auction can cascade.");

            if (CurrentCascadeDepth >= AuctionConstants.MaxCascadeDepth)
                throw new InvalidOperationException("Maximum cascade depth reached.");

            if (nextWinnerId == Guid.Empty)
                throw new ArgumentException("Next winner id is required.", nameof(nextWinnerId));

            if (newDeadline.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Deadline must be UTC.", nameof(newDeadline));

            CurrentCascadeDepth++;
            Status = AuctionStatus.Defaulted_Cascaded;
            Status = AuctionStatus.Pending_Payment;
            WinnerId = nextWinnerId;
            PaymentDeadline = newDeadline;
            MarkUpdated();
        }

        public void FullyDefault()
        {
            if (Status != AuctionStatus.Pending_Payment)
                throw new InvalidOperationException("Only pending payment auction can be fully defaulted.");

            Status = AuctionStatus.Fully_Defaulted;
            WinnerId = null;
            PaymentDeadline = null;
            MarkUpdated();
        }

        public void SetSettlementJobId(string id)
        {
            SettlementJobId = id;
            MarkUpdated();
        }

        public void SetEndingSoonJobId(string id)
        {
            EndingSoonJobId = id;
            MarkUpdated();
        }

        public bool CanBid()
        {
            return Status == AuctionStatus.Active && DateTime.UtcNow < EndTime;
        }

        public bool CanCancel()
        {
            return Status == AuctionStatus.Active;
        }

        public decimal GetMinNextBid()
        {
            if (!CurrentHighestBid.HasValue)
                return StartingPrice;

            if (MinBidIncrementType == IncrementType.Percentage)
                return CurrentHighestBid.Value * (1 + MinBidIncrementValue);

            return CurrentHighestBid.Value + MinBidIncrementValue;
        }
    }
}