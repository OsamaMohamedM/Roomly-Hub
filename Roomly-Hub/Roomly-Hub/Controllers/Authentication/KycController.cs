using Application.Common.Constants;
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    Errors.Codes.Kyc.AccountInactive => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    Errors.Codes.Kyc.KycLimitReached => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "KYC limit reached")),
                    Errors.Codes.Kyc.PendingKycExists => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Pending KYC already exists")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    Errors.Codes.Kyc.SubmissionNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Submission not found")),
                    Errors.Codes.Kyc.ForbiddenReview => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Forbidden")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }
    }
}