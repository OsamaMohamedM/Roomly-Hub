using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;
using System.IdentityModel.Tokens.Jwt;

namespace Roomly_Hub.Controllers
{
    [Route("api/users")]
    [Authorize]
    public class UserController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var result = await _userService.GetProfileAsync(userId, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto requestDto, CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var result = await _userService.UpdateProfileAsync(userId, requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }
    }
}