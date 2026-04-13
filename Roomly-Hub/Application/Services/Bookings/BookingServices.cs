using Application.Common.Results;
using Application.DTOs.Booking;
using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Bookings;

namespace Application.Services.Bookings
{
    public class BookingServices : IBookingServices
    {
        private readonly IBookingCommandService _bookingCommandService;
        private readonly IBookingQueryService _bookingQueryService;
        private readonly IBookingPaymentFlowService _bookingPaymentFlowService;

        public BookingServices(
            IBookingCommandService bookingCommandService,
            IBookingQueryService bookingQueryService,
            IBookingPaymentFlowService bookingPaymentFlowService)
        {
            _bookingCommandService = bookingCommandService;
            _bookingQueryService = bookingQueryService;
            _bookingPaymentFlowService = bookingPaymentFlowService;
        }

        public Task<Result<BookingSummaryDto>> MarkBookingAsPaidByInvoiceIdAsync(string invoiceId, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.MarkBookingAsPaidByInvoiceIdAsync(invoiceId, cancellation);

        public Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default)
            => _bookingCommandService.CancelBookingAsync(bookingCancelRequestDto, cancellation);

        public Task<Result<CreateBookingResponseDto>> CreateBookingAsync(Guid guestId, CreateBookingDto createBookingDto, CancellationToken cancellation = default)
            => _bookingCommandService.CreateBookingAsync(guestId, createBookingDto, cancellation);

        public Task<Result<EInvoiceResponseData>> CreateBookingPaymentInvoiceAsync(Guid guestId, CreateBookingPaymentRequestDto requestDto, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.CreateBookingPaymentInvoiceAsync(guestId, requestDto, cancellation);

        public Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default)
            => _bookingQueryService.GetBookingsByGuestAsync(guestId, cancellation);

        public Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default)
            => _bookingQueryService.GetBookingSummaryAsync(bookingId, cancellation);

        public Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
            => _bookingCommandService.UpdateBookingAsync(bookingRequestDto, cancellation);

        public Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default)
            => _bookingQueryService.GetPendingRequestsForHostAsync(hostId, cancellation);

        public Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
            => _bookingCommandService.ApproveBookingRequestAsync(hostId, bookingId, cancellation);

        public Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
            => _bookingCommandService.RejectBookingRequestAsync(hostId, bookingId, cancellation);

        public Task<Result<BookingSummaryDto>> MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.MarkBookingAsPaidAsync(bookingId, cancellation);

        public Task<Result<BookingSummaryDto>> MarkBookingAsRefundedAsync(Guid bookingId, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.MarkBookingAsRefundedAsync(bookingId, cancellation);

        public Task<Result<IEnumerable<HostBookingRequestDto>>> GetHostRoomBookingsAsync(Guid hostId, Guid roomId, DateTime from, DateTime to, CancellationToken cancellation = default)
            => _bookingQueryService.GetHostRoomBookingsAsync(hostId, roomId, from, to, cancellation);

        public Task<Result<BookingPaymentLinkResponseDto>> GetBookingPaymentLinkAsync(Guid guestId, Guid bookingId, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.GetBookingPaymentLinkAsync(guestId, bookingId, cancellation);

        public Task<Result<BookingPaymentLinkResponseDto>> InitiatePaymentAsync(Guid guestId, Guid bookingId, InitiatePaymentDto initiatePaymentDto, CancellationToken cancellation = default)
            => _bookingPaymentFlowService.InitiatePaymentAsync(guestId, bookingId, initiatePaymentDto, cancellation);
    }
}