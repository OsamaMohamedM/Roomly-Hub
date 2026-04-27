using Application.Interfaces.Services;
using Domain.enums.Notifications;
using MediatR;

namespace Application.Events.Notifications
{
    public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
    {
        private readonly INotificationService _notificationService;

        public UserRegisteredEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.CreateDefaultPreferencesAsync(notification.UserId, cancellationToken);
        }
    }

    public class BookingRequestedEventHandler : INotificationHandler<BookingRequestedEvent>
    {
        private readonly INotificationService _notificationService;

        public BookingRequestedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(BookingRequestedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.HostId, NotificationType.BookingRequested, "New booking request", "A new booking request was created for your room.", $"/host/bookings/{notification.BookingId}", cancellationToken);
        }
    }

    public class BookingConfirmedEventHandler : INotificationHandler<BookingConfirmedEvent>
    {
        private readonly INotificationService _notificationService;

        public BookingConfirmedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(BookingConfirmedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.GuestId, NotificationType.BookingConfirmed, "Booking confirmed", "Your booking request has been confirmed by the host.", $"/bookings/{notification.BookingId}", cancellationToken);
        }
    }

    public class BookingRejectedEventHandler : INotificationHandler<BookingRejectedEvent>
    {
        private readonly INotificationService _notificationService;

        public BookingRejectedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(BookingRejectedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.GuestId, NotificationType.BookingRejected, "Booking rejected", "Your booking request has been rejected by the host.", $"/bookings/{notification.BookingId}", cancellationToken);
        }
    }

    public class BookingCancelledEventHandler : INotificationHandler<BookingCancelledEvent>
    {
        private readonly INotificationService _notificationService;

        public BookingCancelledEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(BookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                _notificationService.SendAsync(notification.HostId, NotificationType.BookingCancelled, "Booking cancelled", "A guest cancelled the booking.", $"/host/bookings/{notification.BookingId}", cancellationToken),
                _notificationService.SendAsync(notification.GuestId, NotificationType.BookingCancelled, "Booking cancelled", "Your booking has been cancelled.", $"/bookings/{notification.BookingId}", cancellationToken));
        }
    }

    public class PaymentConfirmationEventHandler : INotificationHandler<PaymentConfirmationEvent>
    {
        private readonly INotificationService _notificationService;

        public PaymentConfirmationEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(PaymentConfirmationEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.GuestId, NotificationType.PaymentConfirmation, "Payment confirmed", "Your payment was confirmed and your booking is now confirmed.", $"/bookings/{notification.BookingId}", cancellationToken);
        }
    }

    public class PaymentFailedEventHandler : INotificationHandler<PaymentFailedEvent>
    {
        private readonly INotificationService _notificationService;

        public PaymentFailedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.GuestId, NotificationType.PaymentFailed, "Payment failed", "Payment failed. You can retry payment.", $"/bookings/{notification.BookingId}/payment", cancellationToken);
        }
    }

    public class KycApprovedEventHandler : INotificationHandler<KycApprovedEvent>
    {
        private readonly INotificationService _notificationService;

        public KycApprovedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(KycApprovedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.UserId, NotificationType.KycApproved, "KYC approved", "Your KYC submission has been approved.", $"/kyc/submissions/{notification.SubmissionId}", cancellationToken);
        }
    }

    public class KycRejectedEventHandler : INotificationHandler<KycRejectedEvent>
    {
        private readonly INotificationService _notificationService;

        public KycRejectedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(KycRejectedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.UserId, NotificationType.KycRejected, "KYC rejected", "Your KYC submission has been rejected.", $"/kyc/submissions/{notification.SubmissionId}", cancellationToken);
        }
    }

    public class ReviewReceivedEventHandler : INotificationHandler<ReviewReceivedEvent>
    {
        private readonly INotificationService _notificationService;

        public ReviewReceivedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(ReviewReceivedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.RecipientUserId, NotificationType.ReviewReceived, "New review received", "You received a new review.", $"/reviews/{notification.ReviewId}", cancellationToken);
        }
    }

    public class AccountLockedEventHandler : INotificationHandler<AccountLockedEvent>
    {
        private readonly INotificationService _notificationService;

        public AccountLockedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(AccountLockedEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.UserId, NotificationType.AccountLocked, "Account locked", "Your account has been locked due to multiple failed login attempts.", "/auth/request-unlock", cancellationToken);
        }
    }

    public class SecurityAlertEventHandler : INotificationHandler<SecurityAlertEvent>
    {
        private readonly INotificationService _notificationService;

        public SecurityAlertEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Handle(SecurityAlertEvent notification, CancellationToken cancellationToken)
        {
            await _notificationService.SendAsync(notification.UserId, NotificationType.SecurityAlert, "Security alert", "A security event was detected on your account. Please login again.", "/auth/login", cancellationToken);
        }
    }
}