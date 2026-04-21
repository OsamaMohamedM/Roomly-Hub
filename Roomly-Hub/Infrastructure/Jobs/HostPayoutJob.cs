using Application.Interfaces.Services.Wallet;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs
{
    public class HostPayoutJob
    {
        private readonly IWalletCommandService _walletCommandService;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<HostPayoutJob> _logger;

        public HostPayoutJob(
            IWalletCommandService walletCommandService,
            IBookingRepository bookingRepository,
            ILogger<HostPayoutJob> logger)
        {
            _walletCommandService = walletCommandService;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid bookingId)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.Completed)
            {
                _logger.LogWarning("Host payout skipped for booking {BookingId}. Booking missing or not completed", bookingId);
                return;
            }

            var hostId = booking.Room.HostId;
            var result = await _walletCommandService.PayoutHostAsync(hostId, bookingId, booking.TotalPrice, CancellationToken.None);

            if (result.IsFailure)
            {
                _logger.LogWarning("Host payout failed for booking {BookingId}. ErrorCode: {ErrorCode}", bookingId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("Host payout completed for booking {BookingId}", bookingId);
        }
    }
}