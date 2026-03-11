using FluentValidation.Results;

namespace Application.Common.Helpers
{
    internal static class ValidationHelper
    {
        internal static Dictionary<string, string[]> ToErrorDictionary(ValidationResult validationResult)
        {
            return validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        }
    }
}
