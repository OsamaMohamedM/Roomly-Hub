using Application.Common.Constants;
using Application.DTOs.Booking;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Bookings
{
    [Authorize]
    [Route("api/bookings")]
    public class BookingController : ApiControllerBase
    {
        private readonly IBookingServices _bookingServices;

        public BookingController(IBookingServices bookingServices)
        {
            _bookingServices = bookingServices;
        }

        [HttpGet("host/requests")]
        public async Task<IActionResult> GetPendingRequestsForHost(CancellationToken cancellationToken)
        {
            var hostId = GetUserId();
            if (hostId == null)
            {
                return Unauthorized();
            }

            var result = await _bookingServices.GetPendingRequestsForHostAsync(hostId.Value, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpPost("{bookingId:guid}/approve")]
        public async Task<IActionResult> ApproveBookingRequest(Guid bookingId, CancellationToken cancellationToken)
        {
            var hostId = GetUserId();
            if (hostId == null)
            {
                return Unauthorized();
            }

            var result = await _bookingServices.ApproveBookingRequestAsync(hostId.Value, bookingId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    var code when code == Errors.Codes.Booking.InvalidBookingState => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid booking state")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("{bookingId:guid}/reject")]
        public async Task<IActionResult> RejectBookingRequest(Guid bookingId, CancellationToken cancellationToken)
        {
            var hostId = GetUserId();
            if (hostId == null)
            {
                return Unauthorized();
            }

            var result = await _bookingServices.RejectBookingRequestAsync(hostId.Value, bookingId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    var code when code == Errors.Codes.Booking.InvalidBookingState => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid booking state")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            dto.UserId = userId.Value;
            dto.BookingId = null;

            var result = await _bookingServices.CreateBookingAsync(dto, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpPut("{bookingId:guid}")]
        public async Task<IActionResult> UpdateBooking(Guid bookingId, [FromBody] BookingRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            dto.BookingId = bookingId;
            dto.UserId = userId.Value;

            var result = await _bookingServices.UpdateBookingAsync(dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    var code when code == Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    var code when code == Errors.Codes.Booking.RoomNotAvailable => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Room not available")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("{bookingId:guid}/cancel")]
        public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var dto = new BookingCancelRequestDto
            {
                BookingId = bookingId,
                GuestId = userId.Value
            };

            var result = await _bookingServices.CancelBookingAsync(dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    var code when code == Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    var code when code == Errors.Codes.Booking.CancellationNotAllowed => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Cancellation not allowed")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _bookingServices.GetBookingsByGuestAsync(userId.Value, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpGet("{bookingId:guid}")]
        public async Task<IActionResult> GetBookingSummary(Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _bookingServices.GetBookingSummaryAsync(bookingId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    var code when code == Errors.Codes.Booking.BookingNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Booking not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }
    }
}