using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Roomly_Hub.Common
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        
        protected Guid? GetUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
                return null;

            return userId;
        }

        protected ApiProblemDetails CreateProblemDetails(Result result, int status, string title)
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