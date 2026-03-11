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
        private readonly IRegisterService _registerService;
        private readonly ILoginService _loginService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IGoogleLoginService _googleLoginService;

        public AuthController(
            IRegisterService registerService,
            ILoginService loginService,
            IEmailVerificationService emailVerificationService,
            IRefreshTokenService refreshTokenService,
            IGoogleLoginService googleLoginService)
        {
            _registerService = registerService;
            _loginService = loginService;
            _emailVerificationService = emailVerificationService;
            _refreshTokenService = refreshTokenService;
            _googleLoginService = googleLoginService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _registerService.RegisterAsync(requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "EMAIL_ALREADY_EXISTS" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Email already in use")),
                    "OTP_COOLDOWN" => StatusCode(StatusCodes.Status429TooManyRequests, CreateProblemDetails(result, StatusCodes.Status429TooManyRequests, "Too many requests")),
                    "EMAIL_SEND_FAILED" => StatusCode(StatusCodes.Status502BadGateway, CreateProblemDetails(result, StatusCodes.Status502BadGateway, "Email delivery failed")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] OtpVerifyDto otpVerifyDto, CancellationToken cancellationToken)
        {
            var result = await _emailVerificationService.VerifyEmailAsync(otpVerifyDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    "EMAIL_ALREADY_VERIFIED" => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Already verified")),
                    "INVALID_OTP" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid OTP")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _loginService.LoginAsync(requestDto, cancellationToken);

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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _refreshTokenService.RefreshTokenAsync(requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "INVALID_REFRESH_TOKEN" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Invalid token")),
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    "ACCOUNT_LOCKED" => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _googleLoginService.LoginWithGoogleAsync(requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "INVALID_GOOGLE_TOKEN" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Invalid Google token")),
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
                problem.Extensions["errors"] = result.Errors;

            return problem;
        }
    }
}