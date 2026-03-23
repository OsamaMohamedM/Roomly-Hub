using Application.DTOs.Rooms;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;
using System.IdentityModel.Tokens.Jwt;

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
            var result = await _roomModerationService.GetPendingRoomsAsync(cancellationToken);
            if (result.IsFailure)
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));

            return Ok(result.Value);
        }

        [HttpPost("{roomId:guid}/approve")]
        public async Task<IActionResult> Approve(Guid roomId, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var moderatorId))
                return Unauthorized();

            var result = await _roomModerationService.ApproveRoomAsync(moderatorId, roomId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "RoomNotFound" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("{roomId:guid}/reject")]
        public async Task<IActionResult> Reject(Guid roomId, [FromBody] RejectRoomRequestDto dto, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var moderatorId))
                return Unauthorized();

            var result = await _roomModerationService.RejectRoomAsync(moderatorId, roomId, dto.Reason, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "RoomNotFound" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        private bool TryGetUserId(out Guid userId)
        {
            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(userIdValue, out userId);
        }
    }
}
