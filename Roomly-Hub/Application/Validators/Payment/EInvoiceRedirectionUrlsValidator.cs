using Application.DTOs.Payment.FawaterkRequest;
using FluentValidation;

namespace Application.Validators.Payment
{
    public class EInvoiceRedirectionUrlsValidator : AbstractValidator<EInvoiceRedirectionUrls>
    {
        public EInvoiceRedirectionUrlsValidator()
        {
            RuleFor(x => x.OnSuccess)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.OnSuccess));

            RuleFor(x => x.OnFailure)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.OnFailure));

            RuleFor(x => x.OnPending)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.OnPending));

            RuleFor(x => x)
                .Must(x => IsValidUrlOrEmpty(x.OnSuccess) && IsValidUrlOrEmpty(x.OnFailure) && IsValidUrlOrEmpty(x.OnPending))
                .WithMessage("Redirection URLs must be valid absolute URLs.");
        }

        private static bool IsValidUrlOrEmpty(string? url)
        {
            return string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
