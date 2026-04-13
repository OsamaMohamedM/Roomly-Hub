using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Booking;
using Application.DTOs.Payment;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Bookings;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services.Bookings
{
    public class BookingPaymentFlowService : IBookingPaymentFlowService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly IBookingPaymentRequestFactory _bookingPaymentRequestFactory;
        private readonly IValidator<CreateBookingPaymentRequestDto> _createBookingPaymentValidator;
        private readonly IValidator<InitiatePaymentDto> _initiatePaymentValidator;
        private readonly IBookingMapper _bookingMapper;
        private readonly ILogger<BookingPaymentFlowService> _logger;

        public BookingPaymentFlowService(
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            IBookingPaymentRequestFactory bookingPaymentRequestFactory,
            IValidator<CreateBookingPaymentRequestDto> createBookingPaymentValidator,
            IValidator<InitiatePaymentDto> initiatePaymentValidator,
            IBookingMapper bookingMapper,
            ILogger<BookingPaymentFlowService> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _bookingPaymentRequestFactory = bookingPaymentRequestFactory;
            _createBookingPaymentValidator = createBookingPaymentValidator;
            _initiatePaymentValidator = initiatePaymentValidator;
            _bookingMapper = bookingMapper;
            _logger = logger;
        }

        public async Task<Result<BookingSummaryDto>> MarkBookingAsPaidByInvoiceIdAsync(string invoiceId, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            _logger.LogInformation("Marking booking as paid by invoice {InvoiceId}", invoiceId);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByInvoiceIdAsync(invoiceId, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (booking.Status == BookingStatus.Cancelled)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.Status == BookingStatus.PendingHostApproval)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.Status == BookingStatus.Confirmed)
                    {
                        return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                    }

                    booking.MarkPaymentSucceeded();
                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<BookingSummaryDto>> MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Marking booking {BookingId} as paid", bookingId);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
                    }

                    if (booking.Status == BookingStatus.Cancelled)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.Status == BookingStatus.PendingHostApproval)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.Status == BookingStatus.Confirmed)
                    {
                        return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                    }

                    if (booking.PaymentStatus != PaymentStatus.Paid || booking.Status != BookingStatus.Confirmed)
                    {
                        booking.MarkPaymentSucceeded();
                        await _bookingRepository.UpdateBookingAsync(booking, token);
                        await _unitOfWork.SaveChangesAsync(token);
                        _logger.LogInformation("Booking {BookingId} marked as paid", bookingId);
                    }

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking payment concurrency conflict for booking {BookingId}", bookingId);
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<BookingSummaryDto>> MarkBookingAsRefundedAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Marking booking {BookingId} as refunded", bookingId);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
                    }

                    if (booking.PaymentStatus != PaymentStatus.Paid)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    booking.MarkAsRefunded();
                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking refund concurrency conflict for booking {BookingId}", bookingId);
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<EInvoiceResponseData>> CreateBookingPaymentInvoiceAsync(Guid guestId, CreateBookingPaymentRequestDto requestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting payment invoice creation for booking {BookingId} and guest {GuestId}", requestDto.BookingId, guestId);

            var validationResult = await _createBookingPaymentValidator.ValidateAsync(requestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Payment invoice validation failed for booking {BookingId}", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(requestDto.BookingId, cancellation);
            if (booking == null)
            {
                _logger.LogWarning("Payment invoice creation failed. Booking {BookingId} not found", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.GuestId != guestId)
            {
                _logger.LogWarning("Payment invoice unauthorized for booking {BookingId} and guest {GuestId}", requestDto.BookingId, guestId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status != BookingStatus.AwaitingPayment)
            {
                _logger.LogWarning("Payment invoice blocked for booking {BookingId}. Status is {Status}", requestDto.BookingId, booking.Status);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogWarning("Payment invoice skipped for booking {BookingId}. Already paid", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.InvalidBookingState, "Booking is already paid.");
            }

            var paymentRequest = _bookingPaymentRequestFactory.Create(booking, requestDto.PaymentMethodId, requestDto.RedirectionUrls);

            var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
            if (response == null)
            {
                _logger.LogError("Payment invoice creation failed from provider for booking {BookingId}", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.PaymentFailed, Errors.Messages.Booking.PaymentFailed);
            }

            if (!string.IsNullOrWhiteSpace(response.InvoiceId) && !string.IsNullOrWhiteSpace(response.InvoiceKey))
            {
                booking.SetPaymentInvoice(response.InvoiceId, response.InvoiceKey);
                await _bookingRepository.UpdateBookingAsync(booking, cancellation);
                await _unitOfWork.SaveChangesAsync(cancellation);
            }

            _logger.LogInformation("Payment invoice created for booking {BookingId} with invoice key {InvoiceKey}", requestDto.BookingId, response.InvoiceKey);
            return Result<EInvoiceResponseData>.Success(response);
        }

        public async Task<Result<BookingPaymentLinkResponseDto>> GetBookingPaymentLinkAsync(Guid guestId, Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Generating payment link for booking {BookingId} and guest {GuestId}", bookingId, guestId);

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.GuestId != guestId)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status != BookingStatus.AwaitingPayment || booking.PaymentStatus == PaymentStatus.Paid)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            var paymentRequest = _bookingPaymentRequestFactory.Create(booking, null, null);
            var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
            if (response == null || string.IsNullOrWhiteSpace(response.Url) || string.IsNullOrWhiteSpace(response.InvoiceId) || string.IsNullOrWhiteSpace(response.InvoiceKey))
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.PaymentFailed, Errors.Messages.Booking.PaymentFailed);
            }

            booking.SetPaymentInvoice(response.InvoiceId, response.InvoiceKey);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);

            return Result<BookingPaymentLinkResponseDto>.Success(new BookingPaymentLinkResponseDto
            {
                BookingId = booking.Id,
                Status = booking.Status,
                PaymentUrl = response.Url
            });
        }

        public async Task<Result<BookingPaymentLinkResponseDto>> InitiatePaymentAsync(Guid guestId, Guid bookingId, InitiatePaymentDto initiatePaymentDto, CancellationToken cancellation = default)
        {
            var validationResult = await _initiatePaymentValidator.ValidateAsync(initiatePaymentDto, cancellation);
            if (!validationResult.IsValid)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.GuestId != guestId)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status == BookingStatus.PendingHostApproval)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Common.ValidationError, "Booking is waiting for host approval.");
            }

            if (booking.Status != BookingStatus.AwaitingPayment)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.InvalidBookingState, "Booking is already paid.");
            }

            var paymentRequest = _bookingPaymentRequestFactory.Create(booking, initiatePaymentDto.PaymentMethodId, initiatePaymentDto.RedirectionUrls);
            var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
            if (response == null || string.IsNullOrWhiteSpace(response.Url) || string.IsNullOrWhiteSpace(response.InvoiceId) || string.IsNullOrWhiteSpace(response.InvoiceKey))
            {
                return Result<BookingPaymentLinkResponseDto>.Failure(Errors.Codes.Booking.PaymentFailed, Errors.Messages.Booking.PaymentFailed);
            }

            booking.SetPaymentInvoice(response.InvoiceId, response.InvoiceKey);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);

            return Result<BookingPaymentLinkResponseDto>.Success(new BookingPaymentLinkResponseDto
            {
                BookingId = booking.Id,
                Status = booking.Status,
                PaymentUrl = response.Url
            });
        }
    }
}