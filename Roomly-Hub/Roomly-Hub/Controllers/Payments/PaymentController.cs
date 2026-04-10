using Application.DTOs.Booking;
using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Domain.enums.Booking;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;
using System.Text.Json;

namespace Roomly_Hub.Controllers.Payments
{
    [Authorize]
    [Route("api/payments")]
    public class PaymentController : ApiControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingServices _bookingServices;
        private readonly IValidator<WebHookModel> _webhookValidator;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IBookingServices bookingServices,
            IValidator<WebHookModel> webhookValidator,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _bookingServices = bookingServices;
            _webhookValidator = webhookValidator;
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

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] WebHookModel webhook, CancellationToken cancellationToken)
        {
            if (webhook == null)
            {
                return BadRequest("Webhook payload is required.");
            }

            _logger.LogInformation("Received payment webhook for invoice {InvoiceId}", webhook.InvoiceId);

            var validationResult = await _webhookValidator.ValidateAsync(webhook, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Webhook validation failed for invoice {InvoiceId}", webhook.InvoiceId);
                return BadRequest("Invalid webhook payload.");
            }

            if (!_paymentService.VerifyWebhook(webhook))
            {
                _logger.LogWarning("Webhook signature verification failed for invoice {InvoiceId}", webhook.InvoiceId);
                return Unauthorized();
            }

            if (!string.Equals(webhook.InvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(webhook.InvoiceStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Webhook ignored for invoice {InvoiceId} due to non-success status {InvoiceStatus}", webhook.InvoiceId, webhook.InvoiceStatus);
                return BadRequest("Webhook status is not successful.");
            }

            var orderId = webhook.Payload?.OrderId;
            if (string.IsNullOrWhiteSpace(orderId) && !string.IsNullOrWhiteSpace(webhook.PayloadString))
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<WebhookPayload>(webhook.PayloadString);
                    orderId = payload?.OrderId;
                }
                catch (JsonException)
                {
                    return BadRequest("Invalid webhook payload.");
                }
            }

            if (string.IsNullOrWhiteSpace(orderId) || !Guid.TryParse(orderId, out var bookingId))
            {
                return BadRequest("Webhook payload does not contain a valid booking reference.");
            }

            var result = await _bookingServices.MarkBookingAsPaidAsync(bookingId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Webhook processing failed for booking {BookingId}. ErrorCode: {ErrorCode}", bookingId, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.ConcurrencyConflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Concurrency conflict")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            _logger.LogInformation("Webhook processed successfully for booking {BookingId}", bookingId);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("webhook/failed")]
        public IActionResult HandleFailedWebhook([FromBody] FaliledWebHook webhook)
        {
            _logger.LogWarning("Failed payment webhook received for invoice {InvoiceId} with key {InvoiceKey}. Error: {ErrorMessage}", webhook.InvoiceId, webhook.InvoiceKey, webhook.ErrorMessage);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("webhook/cancel")]
        public IActionResult HandleCancelWebhook([FromBody] CancelTransactionModel cancelTransaction)
        {
            if (!_paymentService.VerifyCancelTransaction(cancelTransaction))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Cancellation webhook processed for reference {ReferenceId}", cancelTransaction.ReferenceId);
            return Ok();
        }
    }
}