using Application.Common.Results;
using Application.DTOs.Booking;
using Application.DTOs.Payment;

namespace Application.Interfaces.Services
{
    public interface IBookingServices
    {
        public Task<Result<string>> CreateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default);

        public Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default);

        public Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default);

        public Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);

        public Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> MarkBookingAsRefundedAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<Result<EInvoiceResponseData>> CreateBookingPaymentInvoiceAsync(Guid guestId, CreateBookingPaymentRequestDto requestDto, CancellationToken cancellation = default);

        public Task<Result<IEnumerable<HostBookingRequestDto>>> GetHostRoomBookingsAsync(Guid hostId, Guid roomId, DateTime from, DateTime to, CancellationToken cancellation = default);
    }
}