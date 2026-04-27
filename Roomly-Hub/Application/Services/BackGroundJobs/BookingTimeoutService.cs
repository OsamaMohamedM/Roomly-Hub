using Application.Common.Exceptions;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Booking;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.BackGroundJobs
{
    public class BookingTimeoutService : IBookingTimeoutService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingTimeoutService> _logger;
        private readonly IEmailService _emailService;

        public BookingTimeoutService(IBookingRepository bookingRepository, IUnitOfWork unitOfWork, ILogger<BookingTimeoutService> logger, IEmailService emailService)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task ProcessExpiredHostApprovalsAsync()
        {
            _logger.LogInformation("Starting process to expire pending host approvals");
            var cutoff = DateTime.UtcNow.AddHours(-48);

            try
            {
                var bookingsToExpire = await _bookingRepository.GetPendingHostApprovalsOlderThanAsync(cutoff);
                if (!bookingsToExpire.Any())
                {
                    _logger.LogInformation("No pending host approvals found to expire");
                    return;
                }

                await ExpireHostApprovalBookingsAsync(bookingsToExpire);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict during host approval expiry. Will retry in next cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired host approvals");
            }
        }

        public async Task ProcessUnpaidBookingsAsync()
        {
            _logger.LogInformation("Starting process to expire unpaid bookings");
            var cutoff = DateTime.UtcNow.AddHours(-72);

            try
            {
                var bookingsToExpire = await _bookingRepository.GetUnpaidBookingsOlderThanAsync(cutoff);
                if (!bookingsToExpire.Any())
                {
                    _logger.LogInformation("No unpaid bookings found to expire");
                    return;
                }

                await ExpireUnpaidBookingsAsync(bookingsToExpire);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict during unpaid booking expiry. Will retry in next cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unpaid bookings");
            }
        }

        private async Task ExpireHostApprovalBookingsAsync(IEnumerable<Booking> bookingsToExpire)
        {
            var bookingsList = bookingsToExpire.ToList();
            _logger.LogInformation("Processing {Count} bookings for host approval expiry", bookingsList.Count);

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                foreach (var booking in bookingsList)
                {
                    try
                    {
                        _logger.LogTrace("Expiring booking {BookingId} for room {RoomId} due to host inactivity", booking.Id, booking.RoomId);
                        booking.ExpireDueToHostInactivity();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "Could not expire booking {BookingId} - invalid state transition", booking.Id);
                    }
                }

                await _bookingRepository.UpdateBookingAsync(bookingsList[0], token);
                await _unitOfWork.SaveChangesAsync(token);

                await SendExpiryNotificationsAsync(bookingsList, "host non-approval", token);
            }, new CancellationToken());

            _logger.LogInformation("Completed processing {Count} host approval expirations", bookingsList.Count);
        }

        private async Task ExpireUnpaidBookingsAsync(IEnumerable<Booking> bookingsToExpire)
        {
            var bookingsList = bookingsToExpire.ToList();
            _logger.LogInformation("Processing {Count} bookings for payment expiry", bookingsList.Count);

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                foreach (var booking in bookingsList)
                {
                    try
                    {
                        _logger.LogTrace("Expiring booking {BookingId} for room {RoomId} due to non-payment", booking.Id, booking.RoomId);
                        booking.CancelDueToNonPayment();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "Could not expire booking {BookingId} - invalid state transition", booking.Id);
                    }
                }

                await _bookingRepository.UpdateBookingAsync(bookingsList[0], token);
                await _unitOfWork.SaveChangesAsync(token);

                await SendExpiryNotificationsAsync(bookingsList, "non-payment", token);
            }, new CancellationToken());

            _logger.LogInformation("Completed processing {Count} payment expirations", bookingsList.Count);
        }

        private async Task SendExpiryNotificationsAsync(IEnumerable<Booking> bookings, string expiryReason, CancellationToken ct)
        {
            var bookingsList = bookings.ToList();
            int successCount = 0;
            int failureCount = 0;

            foreach (var booking in bookingsList)
            {
                try
                {
                    if (booking.User?.Email != null && booking.Room?.Title != null)
                    {
                        _logger.LogTrace("Sending expiration email to user {UserId} for booking {BookingId}", booking.GuestId, booking.Id);
                        await _emailService.SendEmailAsync(
                            booking.User.Email,
                            "Booking Request Expired",
                            $"Your booking request for {booking.Room.Title} has expired due to {expiryReason}"
                        );
                        successCount++;
                    }
                    else
                    {
                        _logger.LogWarning("Cannot send expiry notification for booking {BookingId} - missing user email or room title", booking.Id);
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send expiry notification for booking {BookingId}", booking.Id);
                    failureCount++;
                }
            }

            _logger.LogInformation("Expiry notifications sent - Success: {SuccessCount}, Failure: {FailureCount}", successCount, failureCount);
        }
    }
}