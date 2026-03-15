using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;
using System.IdentityModel.Tokens.Jwt;

namespace Roomly_Hub.Controllers
{
    [Route("api/[controller]/v1.0")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailVerificationService _emailVerificationService;

        public AuthController(
            IAuthService authService,
            IEmailVerificationService emailVerificationService)
        {
            _authService = authService;
            _emailVerificationService = emailVerificationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(requestDto, cancellationToken);

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
            var result = await _authService.LoginAsync(requestDto, cancellationToken);

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
            var result = await _authService.RefreshTokenAsync(requestDto, cancellationToken);

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

        [HttpPost("refresh-access-token")]
        public async Task<IActionResult> GenerateNewAccessToken([FromBody] RefreshTokenRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.GenerateNewAccessTokenAsync(requestDto.RefreshToken, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    "ACCOUNT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    "ACCOUNT_LOCKED" => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
                    "SECURITY_ALERT" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Security alert")),
                    "TOKEN_EXPIRED" => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Token expired")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.ForgotPasswordAsync(requestDto.Email, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto requestDto)
        {
            var result = await _authService.ResetPasswordAsync(requestDto);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "INVALID_REQUEST" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid request")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var result = await _authService.LogoutAsync(userId, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    "VALIDATION_ERROR" => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    "USER_NOT_FOUND" => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok();
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequestDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.OAuthWithGoogleAsync(requestDto, cancellationToken);

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
    }
}