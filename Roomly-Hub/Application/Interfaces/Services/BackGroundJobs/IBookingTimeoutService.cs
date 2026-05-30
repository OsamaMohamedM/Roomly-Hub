namespace Application.Services.BackGroundJobs
{
    public interface IBookingTimeoutService
    {
        Task ProcessUnpaidBookingsAsync();

        Task ProcessExpiredHostApprovalsAsync();
    }
}