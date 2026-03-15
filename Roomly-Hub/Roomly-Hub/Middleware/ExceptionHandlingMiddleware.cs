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
                    Detail = validationEx.Message,
                    Instance = httpContext.Request.Path,
                    Extensions = { ["errors"] = validationEx.Errors }
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