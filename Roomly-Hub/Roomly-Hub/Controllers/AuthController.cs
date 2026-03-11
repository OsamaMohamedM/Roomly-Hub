using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

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
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(registerRequestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "EMAIL_ALREADY_EXISTS" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Conflict")),
                    "EMAIL_SEND_FAILED" => StatusCode(StatusCodes.Status502BadGateway, CreateProblemDetails(result, StatusCodes.Status502BadGateway, "Email delivery failed")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken)
        {
            var result = await _authService.VerifyEmailAsync(otpVerifyDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "Not found")),
                    "EMAIL_ALREADY_VERIFIED" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Already verified")),
                    "INVALID_OTP" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid OTP")),
                    "OTP_ALREADY_USED" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "OTP already used")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(loginRequestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "INVALID_CREDENTIALS" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Unauthorized")),
                    "EMAIL_NOT_VERIFIED" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Email not verified")),
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    "ACCOUNT_LOCKED" => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        private ApiProblemDetails CreateProblemDetails(Application.Common.Results.Result result, int status, string title)
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
            {
                problem.Extensions["errors"] = result.Errors;
            }

            return problem;
        }
    }
}