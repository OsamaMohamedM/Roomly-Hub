using Application.Common.Constants;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly_Hub.Common;

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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Auth.EmailAlreadyExists => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Email already in use")),
                    Errors.Codes.Auth.OtpCooldown => StatusCode(StatusCodes.Status429TooManyRequests, CreateProblemDetails(result, StatusCodes.Status429TooManyRequests, "Too many requests")),
                    Errors.Codes.Auth.EmailSendFailed => StatusCode(StatusCodes.Status502BadGateway, CreateProblemDetails(result, StatusCodes.Status502BadGateway, "Email delivery failed")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    Errors.Codes.Auth.EmailAlreadyVerified => Conflict(CreateProblemDetails(result, StatusCodes.Status409Conflict, "Already verified")),
                    Errors.Codes.Auth.InvalidOtp => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid OTP")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Auth.InvalidCredentials => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Unauthorized")),
                    Errors.Codes.Auth.EmailNotVerified => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Email not verified")),
                    Errors.Codes.Auth.AccountInactive => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    Errors.Codes.Auth.AccountLocked => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Auth.InvalidRefreshToken => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Invalid token")),
                    Errors.Codes.Auth.AccountInactive => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    Errors.Codes.Auth.AccountLocked => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
                    Errors.Codes.Auth.AccountInactive => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    Errors.Codes.Auth.AccountLocked => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
                    Errors.Codes.Auth.SecurityAlert => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Security alert")),
                    Errors.Codes.Auth.TokenExpired => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Token expired")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
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
                    Errors.Codes.Auth.InvalidRequest => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid request")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok();
        }

        /// <summary>
        /// Requests an account unlock OTP.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("request-unlock")]
        public async Task<IActionResult> RequestUnlock([FromBody] RequestUnlockDto requestDto, CancellationToken cancellationToken)
        {
            await _authService.RequestAccountUnlockAsync(requestDto, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Unlocks account using unlock OTP.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("unlock-account")]
        public async Task<IActionResult> UnlockAccount([FromBody] UnlockAccountDto requestDto, CancellationToken cancellationToken)
        {
            var result = await _authService.UnlockAccountAsync(requestDto, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Auth.InvalidOtp => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Invalid OTP")),
                    Errors.Codes.Auth.OtpInvalidated => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "OTP invalidated")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok();
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _authService.LogoutAsync(userId.Value, cancellationToken);

            if (result.IsFailure)
            {
                return result.ErrorCode switch
                {
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Common.UserNotFound => NotFound(CreateProblemDetails(result, StatusCodes.Status404NotFound, "User not found")),
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
                    Errors.Codes.Common.ValidationError => BadRequest(CreateProblemDetails(result, StatusCodes.Status400BadRequest, "Validation error")),
                    Errors.Codes.Auth.InvalidGoogleToken => Unauthorized(CreateProblemDetails(result, StatusCodes.Status401Unauthorized, "Invalid Google token")),
                    Errors.Codes.Auth.EmailNotVerified => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Email not verified")),
                    Errors.Codes.Auth.AccountInactive => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result, StatusCodes.Status403Forbidden, "Account inactive")),
                    Errors.Codes.Auth.AccountLocked => StatusCode(StatusCodes.Status423Locked, CreateProblemDetails(result, StatusCodes.Status423Locked, "Account locked")),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, CreateProblemDetails(result, StatusCodes.Status500InternalServerError, "Unexpected error"))
                };
            }

            return Ok(result.Value);
        }
    }
}