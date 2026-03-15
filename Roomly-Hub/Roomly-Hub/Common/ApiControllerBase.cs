using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Roomly_Hub.Common
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
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