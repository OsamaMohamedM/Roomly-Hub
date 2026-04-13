using Application.Common.Constants;
using Application.DTOs.Payment;
using Application.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Webhooks
{
    [AllowAnonymous]
    [Route("api/webhooks/fawaterk")]
    public class FawaterkWebhookController : ApiControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingServices _bookingServices;
        private readonly IValidator<WebHookModel> _webhookValidator;

        public FawaterkWebhookController(
            IPaymentService paymentService,
            IBookingServices bookingServices,
            IValidator<WebHookModel> webhookValidator)
        {
            _paymentService = paymentService;
            _bookingServices = bookingServices;
            _webhookValidator = webhookValidator;
        }

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] WebHookModel webhook, CancellationToken cancellationToken)
        {
            if (webhook == null)
            {
                return BadRequest("Webhook payload is required.");
            }

            var validationResult = await _webhookValidator.ValidateAsync(webhook, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest("Invalid webhook payload.");
            }

            if (!_paymentService.VerifyWebhook(webhook))
            {
                return Unauthorized();
            }

            if (!string.Equals(webhook.InvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(webhook.InvoiceStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return Ok();
            }

            var result = await _bookingServices.MarkBookingAsPaidByInvoiceIdAsync(webhook.InvoiceId.ToString(), cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Errors.Codes.Booking.ConcurrencyConflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Concurrency conflict")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}