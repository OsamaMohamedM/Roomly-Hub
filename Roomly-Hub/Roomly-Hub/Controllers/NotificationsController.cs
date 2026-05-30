using Application.DTOs.Notifications;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

namespace Roomly_Hub.Controllers
{
    [Authorize]
    [Route("api/notifications")]
    [Route("api/v1/notifications")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetUnreadNotificationsAsync(userId.Value, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpGet("unread/count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _notificationService.GetUnreadNotificationsCountAsync(userId.Value, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok(result.Value);
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _notificationService.MarkAllAsReadAsync(userId.Value, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok();
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferenceRequestDto requestDto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _notificationService.UpdatePreferencesAsync(userId.Value, requestDto, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Request failed"));
            }

            return Ok();
        }
    }
}
