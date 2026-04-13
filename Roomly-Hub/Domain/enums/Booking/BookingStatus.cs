namespace Domain.enums.Booking
{
    public enum BookingStatus
    {
        Unknown,

        AwaitingPayment,
        PendingHostApproval,
        Confirmed,
        Cancelled,
        Completed,
        Disputed
    }
}