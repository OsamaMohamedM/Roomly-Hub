using Application.Common.Results;
using Application.DTOs.Booking;
using Application.DTOs.Payment;

namespace Application.Interfaces.Services.Bookings
{
    public interface IBookingPaymentFlowService
    {
        Task<Result<BookingSummaryDto>> MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellation = default);

        Task<Result<BookingSummaryDto>> MarkBookingAsPaidByInvoiceIdAsync(string invoiceId, CancellationToken cancellation = default);

        Task<Result<BookingSummaryDto>> MarkBookingAsRefundedAsync(Guid bookingId, CancellationToken cancellation = default);

        Task<Result<EInvoiceResponseData>> CreateBookingPaymentInvoiceAsync(Guid guestId, CreateBookingPaymentRequestDto requestDto, CancellationToken cancellation = default);

        Task<Result<BookingPaymentLinkResponseDto>> GetBookingPaymentLinkAsync(Guid guestId, Guid bookingId, CancellationToken cancellation = default);

        Task<Result<BookingPaymentLinkResponseDto>> InitiatePaymentAsync(Guid guestId, Guid bookingId, InitiatePaymentDto initiatePaymentDto, CancellationToken cancellation = default);
    }
}