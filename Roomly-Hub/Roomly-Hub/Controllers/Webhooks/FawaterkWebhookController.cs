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
    [Route("api/v1/webhooks/fawaterk")]
    public class FawaterkWebhookController : ApiControllerBase
    {
        private readonly IPaymentWebhookService _paymentWebhookService;

        public FawaterkWebhookController(
            IPaymentWebhookService paymentWebhookService)
        {
            _paymentWebhookService = paymentWebhookService;
        }

        [HttpPost]
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            WebHookModel? webhook;
            try
            {
                webhook = JsonConvert.DeserializeObject<WebHookModel>(rawBody);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

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
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> HandleFailed(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            FaliledWebHook? webhook;
            try
            {
                webhook = JsonConvert.DeserializeObject<FaliledWebHook>(rawBody);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

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
        [RequestSizeLimit(1024 * 1024)]
        public async Task<IActionResult> HandleCancel(CancellationToken cancellationToken)
        {
            var rawBody = await ReadRawBodyAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return BadRequest();
            }

            CancelTransactionModel? cancelTransaction;
            try
            {
                cancelTransaction = JsonConvert.DeserializeObject<CancelTransactionModel>(rawBody);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

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

            if (string.IsNullOrWhiteSpace(rawBody))
                return rawBody;

            if (IsJsonRequest())
            {
                return rawBody;
            }

            if (!IsFormUrlEncodedRequest())
                return null;

            var queryParams = QueryHelpers.ParseQuery(rawBody);
            var dict = queryParams.ToDictionary(k => k.Key, v => v.Value.ToString());
            var jsonString = JsonConvert.SerializeObject(dict);
            return jsonString;
        }

        private bool IsJsonRequest()
        {
            return Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true ||
                   Request.ContentType?.Contains("+json", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool IsFormUrlEncodedRequest()
        {
            return Request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
