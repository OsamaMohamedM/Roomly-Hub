namespace Domain.enums.Notifications
{
    public enum NotificationType
    {
        BookingRequested,
        BookingConfirmed,
        BookingRejected,
        BookingCancelled,
        PaymentConfirmation,
        PaymentFailed,
        ReviewReceived,
        KycApproved,
        KycRejected,
        AccountLocked,
        SecurityAlert
    }
}