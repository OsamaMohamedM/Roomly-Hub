using Application.Common.Constants;
using Application.DTOs.Auctions;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Auctions
{
    [Route("api/auctions")]
    public class AuctionController : ApiControllerBase
    {
        private readonly IAuctionCommandService _auctionCommandService;
        private readonly IAuctionQueryService _auctionQueryService;

        public AuctionController(IAuctionCommandService auctionCommandService, IAuctionQueryService auctionQueryService)
        {
            _auctionCommandService = auctionCommandService;
            _auctionQueryService = auctionQueryService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAuctionAsync([FromBody] CreateAuctionRequestDto dto, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _auctionCommandService.CreateAuctionAsync(userId.Value, dto, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveAuctionsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var result = await _auctionQueryService.GetActiveAuctionsAsync(page, pageSize, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuctionByIdAsync(Guid id, CancellationToken ct)
        {
            var result = await _auctionQueryService.GetAuctionByIdAsync(id, ct);
            if (result.IsFailure)
            {
                if (result.ErrorCode == Errors.Codes.Auction.NotFound)
                    return NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Auction not found"));

                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpPost("{id:guid}/bids")]
        [Authorize]
        public async Task<IActionResult> PlaceBidAsync(Guid id, [FromBody] PlaceBidRequestDto dto, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            dto.AuctionId = id;
            var result = await _auctionCommandService.PlaceBidAsync(userId.Value, dto, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            if (result.Value != null && result.Value.InsufficientFunds)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    error = "INSUFFICIENT_FUNDS_FOR_INSURANCE",
                    topUpUrl = "/api/wallet/top-up",
                    requiredAmount = result.Value.RequiredAmount
                });
            }

            return Ok(result.Value);
        }

        [HttpPost("{id:guid}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelAuctionAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _auctionCommandService.CancelAuctionAsync(userId.Value, id, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok();
        }

        [HttpPost("{id:guid}/pay")]
        [Authorize]
        public async Task<IActionResult> ProcessWinnerPaymentAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _auctionCommandService.ProcessWinnerPaymentAsync(userId.Value, new AuctionPaymentRequestDto { AuctionId = id }, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok();
        }

        [HttpGet("my-bids")]
        [Authorize]
        public async Task<IActionResult> GetMyBidsAsync(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _auctionQueryService.GetMyBidsAsync(userId.Value, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }

        [HttpGet("my-auctions")]
        [Authorize]
        public async Task<IActionResult> GetMyAuctionsAsync(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _auctionQueryService.GetMyAuctionsAsync(userId.Value, ct);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }
    }
}
