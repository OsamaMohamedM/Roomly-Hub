using Application.Common.Exceptions;
using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Roomly_Hub.Common;

namespace Roomly_Hub.Middleware
{
    public class ExceptionHandlingMiddleware : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception at {Path}", httpContext.Request.Path);

            var problemDetails = exception switch
            {
                NotFoundException notFoundEx => new ApiProblemDetails
                {
                    Code = "NOT_FOUND",
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Instance = httpContext.Request.Path
                },
                ForbiddenException forbiddenEx => new ApiProblemDetails
                {
                    Code = "FORBIDDEN",
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = forbiddenEx.Message,
                    Instance = httpContext.Request.Path
                },
                ValidationException validationEx => new ApiProblemDetails
                {
                    Code = "VALIDATION_ERROR",
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Failed",
                    Detail = string.IsNullOrWhiteSpace(validationEx.Message)
                        ? "Request validation failed."
                        : validationEx.Message,
                    Instance = httpContext.Request.Path,
                    Extensions = { ["errors"] = validationEx.Errors }
                },
                ConcurrencyException => new ApiProblemDetails
                {
                    Code = "CONCURRENCY_ERROR",
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency Conflict",
                    Detail = "The resource was modified by another process. Please refresh and try again.",
                    Instance = httpContext.Request.Path
                },
                InvalidOperationException => new ApiProblemDetails
                {
                    Code = "Invalid Attributes",
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Attributes",
                    Detail = exception.Message,
                    Instance = httpContext.Request.Path

                },
                _ => new ApiProblemDetails
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Detail = "Please try again later.",
                    Instance = httpContext.Request.Path
                }
            };

            httpContext.Response.StatusCode = problemDetails.Status!.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}