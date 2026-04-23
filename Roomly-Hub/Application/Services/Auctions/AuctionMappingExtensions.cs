using Application.DTOs.Auctions;
using Domain.Entities.Auctions;

namespace Application.Services.Auctions
{
    public static class AuctionMappingExtensions
    {
        public static AuctionResponseDto ToDto(this Auction auction, string roomTitle)
        {
            return new AuctionResponseDto
            {
                Id = auction.Id,
                RoomId = auction.RoomId,
                CheckInDate = auction.CheckInDate,
                CheckOutDate = auction.CheckOutDate,
                CreatedAt = auction.CreatedAt,
                EndTime = auction.EndTime,
                RoomTitle = roomTitle,
                HostId = auction.HostId,
                StartingPrice = auction.StartingPrice,
                StartTime = auction.StartTime,
                BidCount = auction.Bids?.Count ?? 0,
                CurrentHighestBid = auction.CurrentHighestBid,
                InsuranceDepositAmount = auction.InsuranceDepositAmount,
                MinNextBid = auction.GetMinNextBid(),
                Status = auction.Status.ToString(),
            };
        }

        public static BidResponseDto ToBidDto(this AuctionBid bid)
        {
            return new BidResponseDto
            {
                Id = bid.Id,
                AuctionId = bid.AuctionId,
                Amount = bid.Amount,
                IsWinning = bid.IsWinning,
                Status = bid.Status.ToString(),
                InsuranceLocked = bid.InsuranceLocked,
                PlacedAt = bid.PlacedAt,
            };
        }
    }
}
