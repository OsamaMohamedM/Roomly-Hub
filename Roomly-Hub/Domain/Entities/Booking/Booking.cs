using Domain.Entities.Rooms;
using Domain.enums.Booking;

namespace Domain.Entities.Booking
{
    public class Booking : BaseEntity
    {
        public Booking()
        { }

        public Guid RoomId { get; private set; }
        public Guid GuestId { get; private set; }
        public DateTime CheckInDate { get; private set; }
        public DateTime CheckOutDate { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public decimal TotalPrice { get; private set; }
        public BookingStatus Status { get; private set; }
        public SourceStatus Source { get; private set; }
        public BookingMode BookingMode { get; private set; }
        public CancellationPolicy CancellationPolicy { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public bool IsApprovedByHost { get; private set; }
        public string? PaymentInvoiceId { get; private set; }
        public string? PaymentInvoiceKey { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public Guid? CancelledBy { get; private set; }
        public User User { get; private set; }
        public Room Room { get; private set; }

        public static Booking CreateBooking(
            Guid guestId,
            Guid roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal totalPrice,
            PaymentMethod paymentMethod,
            PaymentStatus paymentStatus,
            BookingMode bookingMode = BookingMode.None,
            SourceStatus sourceStatus = SourceStatus.Unknown,
            CancellationPolicy cancellationPolicy = CancellationPolicy.None)
        {
            if (checkInDate >= checkOutDate)
                throw new ArgumentException("Check-out date must be after check-in date.");

            var initialStatus = bookingMode == BookingMode.RequestAndApprove
                ? BookingStatus.PendingHostApproval
                : BookingStatus.AwaitingPayment;
            var isApprovedByHost = bookingMode != BookingMode.RequestAndApprove;

            return new Booking
            {
                GuestId = guestId,
                RoomId = roomId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                CreatedDate = DateTime.UtcNow,
                TotalPrice = totalPrice,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                BookingMode = bookingMode,
                Source = sourceStatus,
                CancellationPolicy = cancellationPolicy,
                Status = initialStatus,
                IsApprovedByHost = isApprovedByHost
            };
        }

        public void CancelBooking(Guid cancelledBy)
        {
            if (Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Booking is already cancelled.");
            Status = BookingStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancelledBy = cancelledBy;
        }

        public void UpdateBooking(DateTime newCheckIn, DateTime newCheckOut)
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed bookings can be updated.");
            if (newCheckIn >= newCheckOut)
                throw new ArgumentException("Check-out date must be after check-in date.");
            CheckInDate = newCheckIn;
            CheckOutDate = newCheckOut;
        }

        public void MarkAsPaid()
        {
            if (PaymentStatus == PaymentStatus.Paid)
                throw new InvalidOperationException("Booking is already marked as paid.");
            PaymentStatus = PaymentStatus.Paid;
        }

        public void MarkAsRefunded()
        {
            if (PaymentStatus == PaymentStatus.Refunded)
                throw new InvalidOperationException("Booking is already marked as refunded.");
            PaymentStatus = PaymentStatus.Refunded;
        }

        public void MarkAsConfirmed()
        {
            if (Status == BookingStatus.Confirmed)
                throw new InvalidOperationException("Booking is already confirmed.");
            Status = BookingStatus.Confirmed;
        }

        public void ApproveByHost()
        {
            if (Status != BookingStatus.PendingHostApproval)
                throw new InvalidOperationException("Booking is not waiting host approval.");

            IsApprovedByHost = true;
            Status = BookingStatus.AwaitingPayment;
            MarkUpdated();
        }

        public void SetPaymentInvoice(string invoiceId, string invoiceKey)
        {
            PaymentInvoiceId = invoiceId;
            PaymentInvoiceKey = invoiceKey;
            MarkUpdated();
        }

        public void MarkPaymentSucceeded(PaymentMethod paymentMethod)
        {
            if (PaymentStatus != PaymentStatus.Paid)
            {
                PaymentStatus = PaymentStatus.Paid;
            }

            if (Status != BookingStatus.Confirmed)
            {
                Status = BookingStatus.Confirmed;
            }
            PaymentMethod = paymentMethod;
        }

        public void MarkAsPending()
        {
            if (Status == BookingStatus.PendingHostApproval)
                throw new InvalidOperationException("Booking is already pending.");
            Status = BookingStatus.PendingHostApproval;
        }

        public void MarkAsCompleted()
        {
            if (Status == BookingStatus.Completed)
                throw new InvalidOperationException("Booking is already completed.");
            Status = BookingStatus.Completed;
        }

        public void MarkAsCancelled()
        {
            if (Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Booking is already cancelled.");
            Status = BookingStatus.Cancelled;
        }

        public void CancelDueToNonPayment()
        {
            if (Status != BookingStatus.AwaitingPayment)
                throw new InvalidOperationException("Only bookings awaiting payment can be cancelled due to non-payment.");
            Status = BookingStatus.Expired;
            MarkUpdated();
        }

        public void ExpireDueToHostInactivity()
        {
            if (Status != BookingStatus.PendingHostApproval)
                throw new InvalidOperationException("Only bookings pending host approval can expire due to host inactivity.");
            Status = BookingStatus.Expired;
            MarkUpdated();
        }
    }
}