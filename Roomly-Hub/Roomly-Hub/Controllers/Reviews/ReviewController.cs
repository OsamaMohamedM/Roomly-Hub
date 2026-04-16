using Application.Common.Constants;
using Application.DTOs.Reviews;
using Application.Interfaces.Services.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Reviews
{
    [Route("api/reviews")]
    public class ReviewController : ApiControllerBase
    {
        private readonly IReviewCommandService _reviewCommandService;
        private readonly IReviewQueryService _reviewQueryService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewCommandService reviewCommandService,
            IReviewQueryService reviewQueryService,
            ILogger<ReviewController> logger)
        {
            _reviewCommandService = reviewCommandService;
            _reviewQueryService = reviewQueryService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Submit review API called by user {UserId} for booking {BookingId}", userId.Value, dto?.BookingId);
            var result = await _reviewCommandService.SubmitReviewAsync(userId.Value, dto, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Submit review failed for user {UserId}. ErrorCode: {ErrorCode}", userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Review.AlreadySubmitted => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Review already submitted")),
                    var code when code == Errors.Codes.Review.ForbiddenReviewAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Forbidden review action")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpGet("rooms/{roomId:guid}")]
        public async Task<IActionResult> GetRoomReviews(Guid roomId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Get room reviews API called for room {RoomId}", roomId);
            var result = await _reviewQueryService.GetRoomReviewsAsync(roomId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Get room reviews failed for room {RoomId}. ErrorCode: {ErrorCode}", roomId, result.ErrorCode);
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpGet("users/{userId:guid}")]
        public async Task<IActionResult> GetUserReviews(Guid userId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Get user reviews API called for user {UserId}", userId);
            var result = await _reviewQueryService.GetUserReviewsAsync(userId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Get user reviews failed for user {UserId}. ErrorCode: {ErrorCode}", userId, result.ErrorCode);
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("{reviewId:guid}/flag")]
        public async Task<IActionResult> FlagReview(Guid reviewId, [FromBody] FlagReviewRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Flag review API called by user {UserId} for review {ReviewId}", userId.Value, reviewId);
            var result = await _reviewCommandService.FlagReviewAsync(userId.Value, reviewId, dto, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Flag review failed for review {ReviewId} by user {UserId}. ErrorCode: {ErrorCode}", reviewId, userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Review.ReviewNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Review not found")),
                    var code when code == Errors.Codes.Review.ForbiddenReviewAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Forbidden review action")),
                    var code when code == Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [Authorize]
        [HttpDelete("{reviewId:guid}")]
        public async Task<IActionResult> RemoveReview(Guid reviewId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Remove review API called by user {UserId} for review {ReviewId}", userId.Value, reviewId);
            var result = await _reviewCommandService.RemoveReviewAsync(userId.Value, reviewId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Remove review failed for review {ReviewId} by user {UserId}. ErrorCode: {ErrorCode}", reviewId, userId.Value, result.ErrorCode);
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Review.ReviewNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Review not found")),
                    var code when code == Errors.Codes.Review.ForbiddenReviewAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Forbidden review action")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}