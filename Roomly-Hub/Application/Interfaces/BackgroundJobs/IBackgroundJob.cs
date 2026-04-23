namespace Application.Interfaces.BackgroundJobs
{
    public interface IBackgroundJob
    {
        void ScheduleHostPayout(Guid bookingId, TimeSpan delay);

        void ScheduleAuctionSettlement(Guid auctionId, TimeSpan delay);

        void ScheduleAuctionEndingSoon(Guid auctionId, TimeSpan delay);

        void ScheduleAuctionPaymentTimeout(Guid auctionId, TimeSpan delay);
    }
}