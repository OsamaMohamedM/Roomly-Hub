using Application.Common.Constants;
using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
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
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            var webhook = JsonConvert.DeserializeObject<WebHookModel>(rawBody);
            if (webhook == null)
            {
                return BadRequest();
            }

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

        [HttpPost("failed")]
        public async Task<IActionResult> HandleFailed(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            var webhook = JsonConvert.DeserializeObject<FaliledWebHook>(rawBody);
            if (webhook == null)
            {
                return BadRequest();
            }

            var result = await _paymentWebhookService.HandleFailedWebhookAsync(webhook, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> HandleCancel(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            var cancelTransaction = JsonConvert.DeserializeObject<CancelTransactionModel>(rawBody);
            if (cancelTransaction == null)
            {
                return BadRequest();
            }

            var result = await _paymentWebhookService.HandleCancelledWebhookAsync(cancelTransaction, cancellationToken);
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

        private async Task<string?> ReadRawBodyAsync(CancellationToken cancellationToken)
        {
            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;
            var queryParams = QueryHelpers.ParseQuery(rawBody);
            var dict = queryParams.ToDictionary(k => k.Key, v => v.Value.ToString());
            var jsonString = JsonConvert.SerializeObject(dict);
            return jsonString;
        }
    }
}