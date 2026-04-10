using Application.DTOs.Booking;
using Application.DTOs.Payment;
using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Services;
using Domain.enums.Booking;
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

        public PaymentController(IPaymentService paymentService, IBookingServices bookingServices)
        {
            _paymentService = paymentService;
            _bookingServices = bookingServices;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
        {
            var methods = await _paymentService.GetPaymentMethods();
            if (methods == null)
            {
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

            if (requestDto.BookingId == Guid.Empty || requestDto.PaymentMethodId <= 0)
            {
                return BadRequest("BookingId and PaymentMethodId are required.");
            }

            var bookingResult = await _bookingServices.GetBookingSummaryAsync(requestDto.BookingId, cancellationToken);
            if (bookingResult.IsFailure)
            {
                return bookingResult.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(bookingResult, StatusCodes.Status404NotFound, "Booking not found")),
                    _ => BadRequest(CreateProblemDetails(bookingResult, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            var booking = bookingResult.Value;
            if (booking.GuestId != userId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to pay for this booking.");
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                return Conflict("Booking is already paid.");
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return Conflict("Booking must be approved before payment.");
            }

            var paymentRequest = new EInvoiceRequestModel
            {
                PaymentMethodId = requestDto.PaymentMethodId,
                CartItems = new List<CartItemModel>
                {
                    new()
                    {
                        Price = booking.TotalPrice,
                        Quantity = 1
                    }
                },
                PayLoad = new EInvoicePayload
                {
                    OrderId = booking.BookingId.ToString()
                },
                RedirectionUrls = requestDto.RedirectionUrls
            };

            var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
            if (response == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Unable to create payment invoice.");
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] WebHookModel webhook, CancellationToken cancellationToken)
        {
            if (webhook == null)
            {
                return BadRequest("Webhook payload is required.");
            }

            if (!_paymentService.VerifyWebhook(webhook))
            {
                return Unauthorized();
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
                return result.ErrorCode switch
                {
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Application.Common.Constants.Errors.Codes.Booking.ConcurrencyConflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Concurrency conflict")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}