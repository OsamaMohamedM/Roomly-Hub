using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Roomly_Hub.Controllers
{
    [ApiController]
    [Route("api/[controller]/v1.0")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequestDto)
        {
            var result = await _authService.RegisterAsync(registerRequestDto);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage }),
                    "EMAIL_ALREADY_EXISTS" => Conflict(new { code = result.ErrorCode, message = result.ErrorMessage }),
                    "EMAIL_SEND_FAILED" => StatusCode(StatusCodes.Status502BadGateway, new { code = result.ErrorCode, message = result.ErrorMessage }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { code = "UNEXPECTED_ERROR", message = "Unexpected error occurred." })
                };
            }

            return Ok(result.Value);
        }
    }
}