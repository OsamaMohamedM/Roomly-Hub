using Application.Common.Constants;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers.Rooms
{
    [Route("api/rooms")]
    [Authorize]
    public class RoomController : ApiControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.CreateRoomAsync((Guid)userId, dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    Errors.Codes.Common.PermissionDenied => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Permission denied")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPut("{roomId:guid}")]
        public async Task<IActionResult> Update(Guid roomId, [FromBody] UpdateRoomRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.UpdateRoomAsync((Guid)userId, roomId, dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.PermissionDenied => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Permission denied")),
                    Errors.Codes.Common.InvalidState => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid state")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("{roomId:guid}/submit-for-review")]
        public async Task<IActionResult> SubmitForReview(Guid roomId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.SubmitForReviewAsync((Guid)userId, roomId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.PermissionDenied => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Permission denied")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpPost("{roomId:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid roomId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.DeactivateRoomAsync((Guid)userId, roomId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.PermissionDenied => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Permission denied")),
                    Errors.Codes.Common.InvalidState => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid state")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpGet("{roomId:guid}")]
        public async Task<IActionResult> GetById(Guid roomId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var result = await _roomService.GetRoomByIdAsync(roomId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromBody] RoomFilters filters, CancellationToken cancellationToken)
        {
            var result = await _roomService.SearchRoomsAsync(filters, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }
    }
}