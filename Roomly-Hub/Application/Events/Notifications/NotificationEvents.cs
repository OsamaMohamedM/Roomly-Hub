using MediatR;

namespace Application.Events.Notifications
{
    public record UserRegisteredEvent(Guid UserId) : INotification;

    public record BookingRequestedEvent(Guid BookingId, Guid GuestId, Guid HostId, Guid RoomId) : INotification;

    public record BookingConfirmedEvent(Guid BookingId, Guid GuestId, Guid HostId, Guid RoomId) : INotification;

    public record BookingRejectedEvent(Guid BookingId, Guid GuestId, Guid HostId, Guid RoomId) : INotification;

    public record BookingCancelledEvent(Guid BookingId, Guid GuestId, Guid HostId, Guid RoomId) : INotification;

    public record PaymentConfirmationEvent(Guid BookingId, Guid GuestId, string InvoiceReference) : INotification;

    public record PaymentFailedEvent(Guid BookingId, Guid GuestId, string InvoiceReference) : INotification;

    public record KycApprovedEvent(Guid SubmissionId, Guid UserId, Guid ReviewerId) : INotification;

    public record KycRejectedEvent(Guid SubmissionId, Guid UserId, Guid ReviewerId) : INotification;

    public record ReviewReceivedEvent(Guid ReviewId, Guid BookingId, Guid RecipientUserId, Guid ReviewerId) : INotification;

    public record AccountLockedEvent(Guid UserId) : INotification;

    public record SecurityAlertEvent(Guid UserId) : INotification;
}