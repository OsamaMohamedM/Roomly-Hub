using Application.Common.Constants;
using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Webhooks
{
    [AllowAnonymous]
    [Route("api/webhooks/fawaterk")]
    public class FawaterkWebhookController : ApiControllerBase
    {
        private readonly IPaymentWebhookService _paymentWebhookService;

        public FawaterkWebhookController(
            IPaymentWebhookService paymentWebhookService)
        {
            _paymentWebhookService = paymentWebhookService;
        }

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] WebHookModel webhook, CancellationToken cancellationToken)
        {
            var result = await _paymentWebhookService.HandleSuccessWebhookAsync(webhook, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Common.UnauthorizedAction => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Unauthorized")),
                    var code when code == Errors.Codes.Booking.InvalidBookingState => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Invalid booking state")),
                    var code when code == Errors.Codes.Booking.ConcurrencyConflict => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Concurrency conflict")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}