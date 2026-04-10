using Application.Common.Constants;
using Application.DTOs.Rooms;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Rooms
{
    [Authorize]
    [Route("api/rooms/moderation")]
    public class RoomModerationController : ApiControllerBase
    {
        private readonly IRoomModerationService _roomModerationService;

        public RoomModerationController(IRoomModerationService roomModerationService)
        {
            _roomModerationService = roomModerationService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
        {
            var moderatorId = GetUserId();
            if (moderatorId == Guid.Empty || moderatorId == null)
                return Unauthorized();
            var result = await _roomModerationService.GetPendingRoomsAsync((Guid)moderatorId, cancellationToken);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }

        [HttpPost("{roomId:guid}/approve")]
        public async Task<IActionResult> Approve(Guid roomId, CancellationToken cancellationToken)
        {
            var moderatorId = GetUserId();
            if (moderatorId == Guid.Empty || moderatorId == null)
                return Unauthorized();

            var result = await _roomModerationService.ApproveRoomAsync((Guid)moderatorId, roomId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Room.ModeratorNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Moderator not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    Errors.Codes.Room.InvalidRoomStatus => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid room status")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("{roomId:guid}/reject")]
        public async Task<IActionResult> Reject(Guid roomId, [FromBody] RejectRoomRequestDto dto, CancellationToken cancellationToken)
        {
            var moderatorId = GetUserId();
            if (moderatorId == Guid.Empty || moderatorId == null)
                return Unauthorized();

            var result = await _roomModerationService.RejectRoomAsync((Guid)moderatorId, roomId, dto.Reason, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Room.ModeratorNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Moderator not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    Errors.Codes.Room.InvalidRoomStatus => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid room status")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}