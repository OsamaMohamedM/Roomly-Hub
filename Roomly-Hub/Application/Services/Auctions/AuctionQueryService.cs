using Application.Common.Constants;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.Auctions;
using Application.Interfaces.Services;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions
{
    public class AuctionQueryService : IAuctionQueryService
    {
        private readonly IAuctionRepository _auctionRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly ILogger<AuctionQueryService> _logger;

        public AuctionQueryService(
            IAuctionRepository auctionRepository,
            IRoomRepository roomRepository,
            ILogger<AuctionQueryService> logger)
        {
            _auctionRepository = auctionRepository;
            _roomRepository = roomRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<AuctionSummaryDto>>> GetActiveAuctionsAsync(int page, int pageSize, CancellationToken ct)
        {
            if (page <= 0 || pageSize <= 0)
                return Result<PagedResult<AuctionSummaryDto>>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var total = await _auctionRepository.CountActiveAsync(ct);
            var paged = await _auctionRepository.GetActivePagedAsync(page, pageSize, ct);
            var items = new List<AuctionSummaryDto>(paged.Count);

            foreach (var auction in paged)
            {
                var room = await _roomRepository.GetByIdAsync(auction.RoomId, ct);
                items.Add(new AuctionSummaryDto
                {
                    Id = auction.Id,
                    RoomTitle = room?.Title ?? string.Empty,
                    CheckInDate = auction.CheckInDate,
                    CheckOutDate = auction.CheckOutDate,
                    CurrentHighestBid = auction.CurrentHighestBid,
                    InsuranceDepositAmount = auction.InsuranceDepositAmount,
                    EndTime = auction.EndTime,
                    Status = auction.Status.ToString(),
                    BidCount = auction.Bids.Count
                });
            }

            return Result<PagedResult<AuctionSummaryDto>>.Success(new PagedResult<AuctionSummaryDto>(items, total, page, pageSize));
        }

        public async Task<Result<AuctionResponseDto>> GetAuctionByIdAsync(Guid auctionId, CancellationToken ct)
        {
            var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);
            if (auction == null)
                return Result<AuctionResponseDto>.Failure(Errors.Codes.Auction.NotFound, "Auction not found.");

            var room = await _roomRepository.GetByIdAsync(auction.RoomId, ct);
            var dto = new AuctionResponseDto
            {
                Id = auction.Id,
                RoomId = auction.RoomId,
                RoomTitle = room?.Title ?? string.Empty,
                HostId = auction.HostId,
                CheckInDate = auction.CheckInDate,
                CheckOutDate = auction.CheckOutDate,
                StartingPrice = auction.StartingPrice,
                CurrentHighestBid = auction.CurrentHighestBid,
                MinNextBid = auction.GetMinNextBid(),
                InsuranceDepositAmount = auction.InsuranceDepositAmount,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                Status = auction.Status.ToString(),
                BidCount = auction.Bids.Count,
                CreatedAt = auction.CreatedAt
            };

            return Result<AuctionResponseDto>.Success(dto);
        }

        public async Task<Result<List<BidResponseDto>>> GetMyBidsAsync(Guid userId, CancellationToken ct)
        {
            var bids = await _auctionRepository.GetBidsByUserAsync(userId, ct);
            var dtos = bids.Select(b => new BidResponseDto
            {
                Id = b.Id,
                AuctionId = b.AuctionId,
                Amount = b.Amount,
                IsWinning = b.IsWinning,
                Status = b.Status.ToString(),
                InsuranceLocked = b.InsuranceLocked,
                PlacedAt = b.PlacedAt
            }).ToList();

            return Result<List<BidResponseDto>>.Success(dtos);
        }

        public async Task<Result<List<AuctionSummaryDto>>> GetMyAuctionsAsync(Guid hostId, CancellationToken ct)
        {
            var auctions = await _auctionRepository.GetByHostIdAsync(hostId, ct);
            var dtos = new List<AuctionSummaryDto>(auctions.Count);

            foreach (var auction in auctions)
            {
                var room = await _roomRepository.GetByIdAsync(auction.RoomId, ct);
                dtos.Add(new AuctionSummaryDto
                {
                    Id = auction.Id,
                    RoomTitle = room?.Title ?? string.Empty,
                    CheckInDate = auction.CheckInDate,
                    CheckOutDate = auction.CheckOutDate,
                    CurrentHighestBid = auction.CurrentHighestBid,
                    InsuranceDepositAmount = auction.InsuranceDepositAmount,
                    EndTime = auction.EndTime,
                    Status = auction.Status.ToString(),
                    BidCount = auction.Bids.Count
                });
            }

            return Result<List<AuctionSummaryDto>>.Success(dtos);
        }
    }
}
