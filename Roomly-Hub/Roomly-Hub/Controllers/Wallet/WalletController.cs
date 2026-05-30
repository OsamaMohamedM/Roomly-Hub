using Application.Common.Constants;
using Application.DTOs.Wallet;
using Application.Interfaces.Services.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Wallet
{
    [Authorize]
    [Route("api/wallet")]
    [Route("api/v1/wallet")]
    public class WalletController : ApiControllerBase
    {
        private readonly IWalletCommandService _walletCommandService;
        private readonly IWalletQueryService _walletQueryService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletCommandService walletCommandService, IWalletQueryService walletQueryService, ILogger<WalletController> logger)
        {
            _walletCommandService = walletCommandService;
            _walletQueryService = walletQueryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetWallet(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Get wallet API called by user {UserId}", userId.Value);
            var result = await _walletQueryService.GetWalletAsync(userId.Value, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Get wallet failed for user {UserId}. ErrorCode: {ErrorCode}", userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Wallet.NotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Wallet not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Get wallet history API called by user {UserId}. Page: {Page}, PageSize: {PageSize}", userId.Value, page, pageSize);
            var result = await _walletQueryService.GetHistoryAsync(userId.Value, page, pageSize, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Get wallet history failed for user {UserId}. ErrorCode: {ErrorCode}", userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Wallet.NotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Wallet not found")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Wallet top-up API called by user {UserId}. Amount: {Amount}", userId.Value, dto?.Amount);
            var result = await _walletCommandService.TopUpAsync(userId.Value, dto, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Wallet top-up failed for user {UserId}. ErrorCode: {ErrorCode}", userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Wallet.NotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Wallet not found")),
                    var code when code == Errors.Codes.Wallet.InvalidAmount => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid amount")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Wallet withdraw API called by user {UserId}. Amount: {Amount}", userId.Value, dto?.Amount);
            var result = await _walletCommandService.WithdrawAsync(userId.Value, dto, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Wallet withdraw failed for user {UserId}. ErrorCode: {ErrorCode}", userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Wallet.NotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Wallet not found")),
                    var code when code == Errors.Codes.Wallet.InsufficientFunds => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Insufficient funds")),
                    var code when code == Errors.Codes.Wallet.WithdrawMinimum => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Below minimum withdrawal")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}
