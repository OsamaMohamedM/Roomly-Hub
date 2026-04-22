using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Auctions.Hubs
{
    public class AuctionHub : Hub
    {
        private readonly ILogger<AuctionHub> _logger;

        public AuctionHub(ILogger<AuctionHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinAuctionAsync(string auctionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
            _logger.LogInformation("Connection {ConnectionId} joined auction group {AuctionId}", Context.ConnectionId, auctionId);
        }

        public async Task LeaveAuctionAsync(string auctionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
            _logger.LogInformation("Connection {ConnectionId} left auction group {AuctionId}", Context.ConnectionId, auctionId);
        }
    }
}