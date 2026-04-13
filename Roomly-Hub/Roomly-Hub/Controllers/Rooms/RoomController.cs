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

        [HttpPost("{roomId:guid}/activate")]
        public async Task<IActionResult> Activate(Guid roomId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.ActivateRoomAsync((Guid)userId, roomId, cancellationToken);
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

        [HttpDelete("{roomId:guid}")]
        public async Task<IActionResult> Delete(Guid roomId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.DeleteRoomAsync((Guid)userId, roomId, cancellationToken);
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

        [HttpPost("{roomId:guid}/photos")]
        public async Task<IActionResult> AddPhoto(Guid roomId, [FromBody] AddRoomPhotoRequestDto dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.AddRoomPhotoAsync(userId.Value, roomId, dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, result.Errors != null ? result.Errors.ToString() : "Validation Errors")),
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpDelete("{roomId:guid}/photos/{photoId:guid}")]
        public async Task<IActionResult> RemovePhoto(Guid roomId, Guid photoId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _roomService.RemoveRoomPhotoAsync(userId.Value, roomId, photoId, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
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

        [HttpGet("{roomId:guid}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailability(Guid roomId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
        {
            var result = await _roomService.GetAvailabilityAsync(roomId, year, month, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("{roomId:guid}/block-dates")]
        public async Task<IActionResult> BlockDates(Guid roomId, [FromBody] BlockDatesRequestDto dto, CancellationToken cancellationToken)
        {
            var hostId = GetUserId();
            if (hostId == null)
                return Unauthorized();

            var result = await _roomService.BlockDatesAsync(hostId.Value, roomId, dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    Errors.Codes.Room.DateAlreadyBlocked => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Date already blocked")),
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }

        [HttpDelete("{roomId:guid}/block-dates")]
        public async Task<IActionResult> UnblockDates(Guid roomId, [FromBody] BlockDatesRequestDto dto, CancellationToken cancellationToken)
        {
            var hostId = GetUserId();
            if (hostId == null)
                return Unauthorized();

            var result = await _roomService.UnblockDatesAsync(hostId.Value, roomId, dto, cancellationToken);
            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Room.RoomNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Room not found")),
                    Errors.Codes.Common.UnauthorizedAction => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Unauthorized action")),
                    Errors.Codes.Room.DateNotBlocked => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Date not blocked")),
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"))
                };
            }

            return Ok();
        }
    }
}