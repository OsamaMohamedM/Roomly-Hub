using Microsoft.AspNetCore.Mvc;

namespace Roomly_Hub.Common
{
    public class ApiProblemDetails : ProblemDetails
    {
        public string? Code { get; init; }
    }
}