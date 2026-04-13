using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs.Payment;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Application.Services.Payments
{
    public class PaymentWebhookService : IPaymentWebhookService
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<WebHookModel> _webhookValidator;
        private readonly ILogger<PaymentWebhookService> _logger;

        public PaymentWebhookService(
            IPaymentService paymentService,
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IValidator<WebHookModel> webhookValidator,
            ILogger<PaymentWebhookService> logger)
        {
            _paymentService = paymentService;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _webhookValidator = webhookValidator;
            _logger = logger;
        }

        public async Task<Result> HandleSuccessWebhookAsync(WebHookModel webhook, CancellationToken cancellationToken = default)
        {
            if (webhook == null)
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            _logger.LogInformation("Webhook processing started for invoice {InvoiceId}", webhook.InvoiceId);

            var validationResult = await _webhookValidator.ValidateAsync(webhook, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Webhook validation failed for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));
            }

            if (!_paymentService.VerifyWebhook(webhook))
            {
                _logger.LogWarning("Webhook signature verification failed for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            if (!string.Equals(webhook.InvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(webhook.InvoiceStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Webhook ignored for invoice {InvoiceId} with status {Status}", webhook.InvoiceId, webhook.InvoiceStatus);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(webhook.InvoiceId.ToString(), cancellationToken);
            if (booking == null)
            {
                _logger.LogWarning("Booking not found for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                _logger.LogInformation("Webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                return Result.Success();
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.PendingHostApproval)
            {
                _logger.LogWarning("Webhook cannot process booking {BookingId} in status {Status}", booking.Id, booking.Status);
                return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var latestBooking = await _bookingRepository.GetBookingByIdAsync(booking.Id, token);
                    if (latestBooking == null)
                    {
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (latestBooking.PaymentStatus == PaymentStatus.Paid || latestBooking.Status == BookingStatus.Confirmed)
                    {
                        _logger.LogInformation("Webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                        return Result.Success();
                    }

                    latestBooking.MarkPaymentSucceeded();
                    await _bookingRepository.UpdateBookingAsync(latestBooking, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    _logger.LogInformation("Webhook processed successfully for booking {BookingId}", latestBooking.Id);
                    return Result.Success();
                }, cancellationToken);
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Webhook concurrency conflict for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> HandleFailedWebhookAsync(FaliledWebHook webhook, CancellationToken cancellationToken = default)
        {
            if (webhook == null)
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            _logger.LogInformation("Failed webhook processing started for invoice {InvoiceId}", webhook.InvoiceId);

            var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(webhook.InvoiceId.ToString(), cancellationToken);
            if (booking == null)
            {
                _logger.LogWarning("Booking not found for failed invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                _logger.LogInformation("Failed webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                return Result.Success();
            }

            if (booking.Status == BookingStatus.AwaitingPayment)
            {
                _logger.LogInformation("Failed webhook received for booking {BookingId}. Booking remains awaiting payment", booking.Id);
            }

            return Result.Success();
        }

        public async Task<Result> HandleCancelledWebhookAsync(CancelTransactionModel cancelTransaction, CancellationToken cancellationToken = default)
        {
            if (cancelTransaction == null)
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            _logger.LogInformation("Cancellation webhook processing started for reference {ReferenceId}", cancelTransaction.ReferenceId);

            if (!_paymentService.VerifyCancelTransaction(cancelTransaction))
            {
                _logger.LogWarning("Cancellation webhook signature failed for reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            var booking = await ResolveBookingForCancellation(cancelTransaction, cancellationToken);
            if (booking == null)
            {
                _logger.LogWarning("Booking not found for cancellation reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                _logger.LogInformation("Cancellation webhook ignored: booking {BookingId} already cancelled", booking.Id);
                return Result.Success();
            }

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                _logger.LogWarning("Cancellation webhook conflict for booking {BookingId} with paid/confirmed state", booking.Id);
                return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var latestBooking = await _bookingRepository.GetBookingByIdAsync(booking.Id, token);
                    if (latestBooking == null)
                    {
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (latestBooking.Status == BookingStatus.Cancelled)
                    {
                        _logger.LogInformation("Cancellation webhook ignored: booking {BookingId} already cancelled", latestBooking.Id);
                        return Result.Success();
                    }

                    latestBooking.MarkAsCancelled();
                    await _bookingRepository.UpdateBookingAsync(latestBooking, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    _logger.LogInformation("Cancellation webhook processed for booking {BookingId}", latestBooking.Id);
                    return Result.Success();
                }, cancellationToken);
            }
            catch (ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Cancellation webhook concurrency conflict for reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        private async Task<Domain.Entities.Booking.Booking?> ResolveBookingForCancellation(CancelTransactionModel cancelTransaction, CancellationToken cancellationToken)
        {
            if (Guid.TryParse(cancelTransaction.ReferenceId, out var bookingId))
            {
                return await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
            }

            var byInvoice = await _bookingRepository.GetBookingByInvoiceIdAsync(cancelTransaction.ReferenceId, cancellationToken);
            if (byInvoice != null)
            {
                return byInvoice;
            }

            if (cancelTransaction.PayLoad is JObject payload)
            {
                var orderId = payload["orderId"]?.ToString() ?? payload["order_id"]?.ToString();
                if (!string.IsNullOrWhiteSpace(orderId) && Guid.TryParse(orderId, out var payloadBookingId))
                {
                    return await _bookingRepository.GetBookingByIdAsync(payloadBookingId, cancellationToken);
                }
            }

            return null;
        }
    }
}