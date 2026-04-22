using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs
{
    public class AuctionPaymentTimeoutJob
    {
        private readonly IAuctionCommandService _auctionCommandService;
        private readonly ILogger<AuctionPaymentTimeoutJob> _logger;

        public AuctionPaymentTimeoutJob(IAuctionCommandService auctionCommandService, ILogger<AuctionPaymentTimeoutJob> logger)
        {
            _auctionCommandService = auctionCommandService;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid auctionId)
        {
            var result = await _auctionCommandService.HandlePaymentTimeoutAsync(auctionId, CancellationToken.None);
            if (result.IsFailure)
            {
                _logger.LogWarning("Auction payment-timeout handling failed for {AuctionId}. ErrorCode: {ErrorCode}", auctionId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("Auction payment-timeout handling finished for {AuctionId}", auctionId);
        }
    }
}
