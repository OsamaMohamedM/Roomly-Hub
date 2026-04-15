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
            _logger.LogInformation("Starting process to expire pending host approvals.");
            DateTime cutoff = DateTime.UtcNow.AddHours(-48);
            var bookingsToExpire = await _bookingRepository.GetPendingHostApprovalsOlderThanAsync(cutoff);
            CancelBooking(bookingsToExpire);
            SendMailsToExpiredBookings(bookingsToExpire, "host non-approval");
        }

        public async Task ProcessUnpaidBookingsAsync()
        {
            _logger.LogInformation("Starting process to expire unpaid bookings.");
            var cutoff = DateTime.UtcNow.AddHours(-72);
            var bookingUnPaid = await _bookingRepository.GetUnpaidBookingsOlderThanAsync(cutoff);
            CancelBooking(bookingUnPaid);
            SendMailsToExpiredBookings(bookingUnPaid, "non-payment");
        }

        private async void CancelBooking(IEnumerable<Booking> bookingsToExpire)
        {
            foreach (var booking in bookingsToExpire)
            {
                _logger.LogTrace("Expiring booking {BookingId} for room {RoomId} due to host non-approval.", booking.Id, booking.RoomId);
                booking.CancelDueToNonPayment();
            }
        }

        private async void SendMailsToExpiredBookings(IEnumerable<Booking> bookingsToExpire, string reasonOfExpires)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    await _unitOfWork.SaveChangesAsync();
                    foreach (var booking in bookingsToExpire)
                    {
                        _logger.LogTrace("Sending expiration email to user {UserId} for booking {BookingId}.", booking.GuestId, booking.Id);
                        await _emailService.SendEmailAsync(
                            booking.User.Email,
                            "Booking Request Expired",
                            $"Your booking request for {booking.Room.Title} has expired due to {reasonOfExpires}"
                        );
                    }
                }
                 );
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict occurred while processing expired host approvals. This is expected if multiple instances of the service are running. Will retry in the next cycle.");
            }
        }
    }
}