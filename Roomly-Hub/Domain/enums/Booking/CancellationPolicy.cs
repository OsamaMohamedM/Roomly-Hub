namespace Domain.enums.Booking
{
    public enum CancellationPolicy
    {
        None = 0,
        FreeCancellation = 1,
        NonRefundable = 2,
        partialRefund = 3,
    }
}