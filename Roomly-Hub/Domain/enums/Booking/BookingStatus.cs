namespace Domain.enums.Booking
{
    public enum BookingStatus
    {
        Unknown,

        AwaitingPayment,
        PendingHostApproval,
        Confirmed,
        Cancelled,
        RejectedByHost,
        Superseded,
        Completed,
        Expired
    }
}
