using Application.Common.Results;
using Application.Common.Constants;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Roomly_Hub.Common
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return null;

            return userId;
        }

        protected ApiProblemDetails CreateProblemDetails(Result result, int status, string title)
        {
            var problem = new ApiProblemDetails
            {
                Code = result.ErrorCode,
                Status = status,
                Title = title,
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            };

            if (result.Errors is not null && result.Errors.Count > 0)
                problem.Extensions["errors"] = result.Errors;

            return problem;
        }

        protected IActionResult ToActionResult(Result result, string fallbackTitle = "Request failed")
        {
            var (status, title) = result.ErrorCode switch
            {
                Errors.Codes.Booking.BookingNotFound => (StatusCodes.Status404NotFound, "Booking not found"),
                Errors.Codes.Room.RoomNotFound => (StatusCodes.Status404NotFound, "Room not found"),
                Errors.Codes.Wallet.NotFound => (StatusCodes.Status404NotFound, "Wallet not found"),
                Errors.Codes.Review.ReviewNotFound => (StatusCodes.Status404NotFound, "Review not found"),
                Errors.Codes.Auction.NotFound => (StatusCodes.Status404NotFound, "Auction not found"),
                Errors.Codes.Common.UnauthorizedAction or Errors.Codes.Common.PermissionDenied => (StatusCodes.Status403Forbidden, "Unauthorized action"),
                Errors.Codes.Common.ValidationError => (StatusCodes.Status400BadRequest, "Validation error"),
                Errors.Codes.Booking.InvalidBookingState or Errors.Codes.Booking.ConcurrencyConflict => (StatusCodes.Status409Conflict, "Conflict"),
                Errors.Codes.Review.AlreadySubmitted => (StatusCodes.Status409Conflict, "Review already submitted"),
                _ => (StatusCodes.Status400BadRequest, fallbackTitle)
            };

            return StatusCode(status, CreateProblemDetails(result, status, title));
        }
    }
}
