using Application.Interfaces.BackgroundJobs;
using Hangfire;

namespace Infrastructure.Jobs
{
    public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
    {
        public void ScheduleHostPayout(Guid bookingId, TimeSpan delay)
        {
            BackgroundJob.Schedule<HostPayoutJob>(
                job => job.ExecuteAsync(bookingId),
                delay);
        }
    }
}
