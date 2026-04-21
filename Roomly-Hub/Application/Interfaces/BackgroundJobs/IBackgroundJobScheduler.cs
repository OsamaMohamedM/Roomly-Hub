namespace Application.Interfaces.BackgroundJobs
{
    public interface IBackgroundJobScheduler
    {
        void ScheduleHostPayout(Guid bookingId, TimeSpan delay);
    }
}
