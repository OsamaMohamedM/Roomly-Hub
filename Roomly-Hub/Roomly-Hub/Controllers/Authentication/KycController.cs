using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;
using System.Security.Claims;

namespace Roomly_Hub.Controllers
{
    [Route("api/kyc")]
    [Authorize]
    public class KycController : ApiControllerBase
    {
        private readonly IKycService _kycService;

        public KycController(IKycService kycService)
        {
            _kycService = kycService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitKycRequestDto requestDto, CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var result = await _kycService.SubmitKycAsync(userId, requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    "KYC_LIMIT_REACHED" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "KYC limit reached")),
                    "PENDING_KYC_EXISTS" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Pending KYC already exists")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("review")]
        public async Task<IActionResult> Review([FromBody] ReviewKycRequestDto requestDto, CancellationToken cancellationToken)
        {
            var reviewerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(reviewerIdValue) || !Guid.TryParse(reviewerIdValue, out var reviewerId))
                return Unauthorized();

            var result = await _kycService.ReviewKycAsync(reviewerId, requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    "SUBMISSION_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Submission not found")),
                    "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Forbidden")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }
    }
}