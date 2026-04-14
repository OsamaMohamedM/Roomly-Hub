using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Payments
{
    [Authorize]
    [Route("api/payments")]
    public class PaymentController : ApiControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingServices _bookingServices;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IBookingServices bookingServices,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _bookingServices = bookingServices;
            _logger = logger;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Loading payment methods");
            var methods = await _paymentService.GetPaymentMethods();
            if (methods == null)
            {
                _logger.LogWarning("Payment methods could not be loaded from provider");
                return StatusCode(StatusCodes.Status502BadGateway, "Unable to load payment methods from the gateway.");
            }

            return Ok(methods);
        }

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateBookingPaymentRequestDto requestDto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            if (requestDto == null)
            {
                return BadRequest("Request body is required.");
            }

            _logger.LogInformation("Creating invoice for booking {BookingId} by user {UserId}", requestDto.BookingId, userId.Value);
            var result = await _bookingServices.CreateBookingPaymentInvoiceAsync(userId.Value, requestDto, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning("Creating invoice failed for booking {BookingId} by user {UserId}. ErrorCode: {ErrorCode}", requestDto.BookingId, userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Application.Common.Constants.Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Application.Common.Constants.Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            _logger.LogInformation("Invoice created successfully for booking {BookingId} by user {UserId}", requestDto.BookingId, userId.Value);
            return Ok(result.Value);
        }

        [HttpGet("bookings/{bookingId:guid}/status")]
        public async Task<IActionResult> GetBookingPaymentStatus(Guid bookingId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _bookingServices.GetBookingSummaryAsync(bookingId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            var booking = result.Value;
            if (booking.GuestId != userId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to access this booking payment status.");
            }

            return Ok(new BookingPaymentStatusDto
            {
                BookingId = booking.BookingId,
                BookingStatus = booking.Status,
                PaymentStatus = booking.PaymentStatus
            });
        }

        [HttpPost("bookings/{bookingId:guid}/refund")]
        public async Task<IActionResult> RefundBookingPayment(Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _bookingServices.MarkBookingAsRefundedAsync(bookingId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.ConcurrencyConflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Concurrency conflict")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

    }
}