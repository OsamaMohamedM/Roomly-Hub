using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs.Payment;
using Application.Events.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Payment;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;

namespace Application.Services.Payments
{
    public class PaymentWebhookService : IPaymentWebhookService
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentWebhookLogRepository _webhookLogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<WebHookModel> _webhookValidator;
        private readonly IPublisher _publisher;
        private readonly ILogger<PaymentWebhookService> _logger;

        public PaymentWebhookService(
            IPaymentService paymentService,
            IBookingRepository bookingRepository,
            IPaymentWebhookLogRepository webhookLogRepository,
            IUnitOfWork unitOfWork,
            IValidator<WebHookModel> webhookValidator,
            IPublisher publisher,
            ILogger<PaymentWebhookService> logger)
        {
            _paymentService = paymentService;
            _bookingRepository = bookingRepository;
            _webhookLogRepository = webhookLogRepository;
            _unitOfWork = unitOfWork;
            _webhookValidator = webhookValidator;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result> HandleSuccessWebhookAsync(WebHookModel webhook, CancellationToken cancellationToken = default)
        {
            if (webhook == null)
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            var isDuplicate = await _webhookLogRepository.IsDuplicateAsync(webhook.InvoiceId, webhook.HashKey, null, WebhookType.Success, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogInformation("Duplicate success webhook ignored for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Success();
            }

            var webhookLog = new PaymentWebhookLog
            {
                InvoiceId = webhook.InvoiceId,
                InvoiceKey = webhook.InvoiceKey,
                HashKey = webhook.HashKey,
                WebhookType = WebhookType.Success,
                RawPayload = JsonConvert.SerializeObject(webhook)
            };

            await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);

            _logger.LogInformation("Webhook processing started for invoice {InvoiceId}", webhook.InvoiceId);

            var validationResult = await _webhookValidator.ValidateAsync(webhook, cancellationToken);
            if (!validationResult.IsValid)
            {
                webhookLog.MarkAsProcessed(false, "Validation failed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Webhook validation failed for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));
            }

            if (!_paymentService.VerifyWebhook(webhook))
            {
                webhookLog.MarkAsProcessed(false, "Signature verification failed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Webhook signature verification failed for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            if (!string.Equals(webhook.InvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(webhook.InvoiceStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                webhookLog.MarkAsProcessed(false, $"Invalid invoice status: {webhook.InvoiceStatus}");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Webhook ignored for invoice {InvoiceId} with status {Status}", webhook.InvoiceId, webhook.InvoiceStatus);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(webhook.InvoiceId.ToString(), cancellationToken);
            if (booking == null)
            {
                webhookLog.MarkAsProcessed(false, "Booking not found");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Booking not found for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            webhookLog.BookingId = booking.Id;
            await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                webhookLog.MarkAsProcessed(true, "Idempotent: Already processed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogInformation("Webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                return Result.Success();
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.PendingHostApproval)
            {
                webhookLog.MarkAsProcessed(false, $"Invalid booking state: {booking.Status}");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
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
                        webhookLog.MarkAsProcessed(false, "Booking not found during processing");
                        await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (latestBooking.PaymentStatus == PaymentStatus.Paid || latestBooking.Status == BookingStatus.Confirmed)
                    {
                        webhookLog.MarkAsProcessed(true, "Idempotent: Already processed");
                        await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                        _logger.LogInformation("Webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                        return Result.Success();
                    }

                    latestBooking.MarkPaymentSucceeded(webhook.PaymentMethod);
                    await _bookingRepository.UpdateBookingAsync(latestBooking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    try
                    {
                        await _publisher.Publish(new PaymentConfirmationEvent(latestBooking.Id, latestBooking.GuestId, webhook.InvoiceId.ToString()), token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish PaymentConfirmationEvent for booking {BookingId}", latestBooking.Id);
                    }

                    webhookLog.MarkAsProcessed(true);
                    await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                    _logger.LogInformation("Webhook processed successfully for booking {BookingId}", latestBooking.Id);
                    return Result.Success();
                }, cancellationToken, IsolationLevel.ReadCommitted);
            }
            catch (ConcurrencyException ex)
            {
                webhookLog.MarkAsProcessed(false, $"Concurrency conflict: {ex.Message}");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning(ex, "Webhook concurrency conflict for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> HandleSuccessWebhookAsync(string invoiceReference, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(invoiceReference))
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(invoiceReference, cancellationToken);
            if (booking == null)
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
                return Result.Success();

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.PendingHostApproval)
                return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var latestBooking = await _bookingRepository.GetBookingByIdAsync(booking.Id, token);
                    if (latestBooking == null)
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);

                    if (latestBooking.PaymentStatus == PaymentStatus.Paid || latestBooking.Status == BookingStatus.Confirmed)
                        return Result.Success();

                    latestBooking.MarkPaymentSucceeded(latestBooking.PaymentMethod);
                    await _bookingRepository.UpdateBookingAsync(latestBooking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    try
                    {
                        await _publisher.Publish(new PaymentConfirmationEvent(latestBooking.Id, latestBooking.GuestId, invoiceReference), token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish PaymentConfirmationEvent for booking {BookingId}", latestBooking.Id);
                    }

                    return Result.Success();
                }, cancellationToken, IsolationLevel.ReadCommitted);
            }
            catch (ConcurrencyException)
            {
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> HandleFailedWebhookAsync(FaliledWebHook webhook, CancellationToken cancellationToken = default)
        {
            if (webhook == null)
            {
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            var isDuplicate = await _webhookLogRepository.IsDuplicateAsync(webhook.InvoiceId, webhook.InvoiceKey, null, WebhookType.Failed, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogInformation("Duplicate failed webhook ignored for invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Success();
            }

            var webhookLog = new PaymentWebhookLog
            {
                InvoiceId = webhook.InvoiceId,
                InvoiceKey = webhook.InvoiceKey,
                WebhookType = WebhookType.Failed,
                RawPayload = JsonConvert.SerializeObject(webhook),
                ErrorMessage = webhook.ErrorMessage
            };

            await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);

            _logger.LogInformation("Failed webhook processing started for invoice {InvoiceId}", webhook.InvoiceId);

            var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(webhook.InvoiceId.ToString(), cancellationToken);
            if (booking == null)
            {
                webhookLog.MarkAsProcessed(false, "Booking not found");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Booking not found for failed invoice {InvoiceId}", webhook.InvoiceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            webhookLog.BookingId = booking.Id;
            await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                webhookLog.MarkAsProcessed(true, "Already processed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogInformation("Failed webhook ignored: Invoice {InvoiceId} already processed", webhook.InvoiceId);
                return Result.Success();
            }

            if (booking.Status == BookingStatus.AwaitingPayment)
            {
                try
                {
                    await _publisher.Publish(new PaymentFailedEvent(booking.Id, booking.GuestId, webhook.InvoiceId.ToString()), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish PaymentFailedEvent for booking {BookingId}", booking.Id);
                }

                webhookLog.MarkAsProcessed(true, "Booking remains AwaitingPayment");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
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

            var isDuplicate = await _webhookLogRepository.IsDuplicateAsync(null, cancelTransaction.HashKey, cancelTransaction.ReferenceId, WebhookType.Cancelled, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogInformation("Duplicate cancellation webhook ignored for reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Success();
            }

            var webhookLog = new PaymentWebhookLog
            {
                ReferenceId = cancelTransaction.ReferenceId,
                HashKey = cancelTransaction.HashKey,
                WebhookType = WebhookType.Cancelled,
                RawPayload = JsonConvert.SerializeObject(cancelTransaction)
            };

            await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);

            _logger.LogInformation("Cancellation webhook processing started for reference {ReferenceId}", cancelTransaction.ReferenceId);

            if (!_paymentService.VerifyCancelTransaction(cancelTransaction))
            {
                webhookLog.MarkAsProcessed(false, "Signature verification failed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Cancellation webhook signature failed for reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            var booking = await ResolveBookingForCancellation(cancelTransaction, cancellationToken);
            if (booking == null)
            {
                webhookLog.MarkAsProcessed(false, "Booking not found");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning("Booking not found for cancellation reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            webhookLog.BookingId = booking.Id;
            await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);

            if (booking.Status == BookingStatus.Cancelled)
            {
                webhookLog.MarkAsProcessed(true, "Idempotent: Already cancelled");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogInformation("Cancellation webhook ignored: booking {BookingId} already cancelled", booking.Id);
                return Result.Success();
            }

            if (booking.PaymentStatus == PaymentStatus.Paid || booking.Status == BookingStatus.Confirmed)
            {
                webhookLog.MarkAsProcessed(false, "Booking is paid/confirmed");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
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
                        webhookLog.MarkAsProcessed(false, "Booking not found during processing");
                        await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (latestBooking.Status == BookingStatus.Cancelled)
                    {
                        webhookLog.MarkAsProcessed(true, "Idempotent: Already cancelled");
                        await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                        _logger.LogInformation("Cancellation webhook ignored: booking {BookingId} already cancelled", latestBooking.Id);
                        return Result.Success();
                    }

                    latestBooking.MarkAsCancelled();
                    await _bookingRepository.UpdateBookingAsync(latestBooking, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    webhookLog.MarkAsProcessed(true);
                    await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                    _logger.LogInformation("Cancellation webhook processed successfully for booking {BookingId}", latestBooking.Id);
                    return Result.Success();
                }, cancellationToken, IsolationLevel.ReadCommitted);
            }
            catch (ConcurrencyException ex)
            {
                webhookLog.MarkAsProcessed(false, $"Concurrency conflict: {ex.Message}");
                await _webhookLogRepository.UpdateAsync(webhookLog, cancellationToken);
                _logger.LogWarning(ex, "Cancellation webhook concurrency conflict for reference {ReferenceId}", cancelTransaction.ReferenceId);
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        private async Task<Domain.Entities.Booking.Booking?> ResolveBookingForCancellation(CancelTransactionModel cancelTransaction, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(cancelTransaction.ReferenceId))
            {
                return await _bookingRepository.GetBookingByInvoiceIdAsync(cancelTransaction.ReferenceId, cancellationToken);
            }

            return null;
        }
    }
}