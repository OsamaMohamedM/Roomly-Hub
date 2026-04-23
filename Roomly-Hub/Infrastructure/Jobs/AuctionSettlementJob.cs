using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs
{
    public class AuctionSettlementJob
    {
        private readonly IAuctionCommandService _auctionCommandService;
        private readonly ILogger<AuctionSettlementJob> _logger;

        public AuctionSettlementJob(IAuctionCommandService auctionCommandService, ILogger<AuctionSettlementJob> logger)
        {
            _auctionCommandService = auctionCommandService;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid auctionId)
        {
            var result = await _auctionCommandService.SettleAuctionAsync(auctionId, CancellationToken.None);
            if (result.IsFailure)
            {
                _logger.LogWarning("Auction settlement failed for {AuctionId}. ErrorCode: {ErrorCode}", auctionId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("Auction settlement finished for {AuctionId}", auctionId);
        }
    }
}