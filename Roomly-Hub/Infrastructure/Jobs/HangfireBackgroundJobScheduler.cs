using Application.Interfaces.BackgroundJobs;
using Hangfire;

namespace Infrastructure.Jobs
{
    public class HangfireBackgroundJobScheduler : IBackgroundJob
    {
        public void ScheduleHostPayout(Guid bookingId, TimeSpan delay)
        {
            BackgroundJob.Schedule<HostPayoutJob>(
                job => job.ExecuteAsync(bookingId),
                delay);
        }

        public void ScheduleAuctionSettlement(Guid auctionId, TimeSpan delay)
        {
            BackgroundJob.Schedule<AuctionSettlementJob>(
                job => job.ExecuteAsync(auctionId),
                delay);
        }

        public void ScheduleAuctionEndingSoon(Guid auctionId, TimeSpan delay)
        {
            BackgroundJob.Schedule<AuctionEndingSoonJob>(
                job => job.ExecuteAsync(auctionId),
                delay);
        }

        public void ScheduleAuctionPaymentTimeout(Guid auctionId, TimeSpan delay)
        {
            BackgroundJob.Schedule<AuctionPaymentTimeoutJob>(
                job => job.ExecuteAsync(auctionId),
                delay);
        }
    }
}