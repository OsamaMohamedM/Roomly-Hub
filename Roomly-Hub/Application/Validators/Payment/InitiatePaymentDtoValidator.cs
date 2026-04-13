using Application.DTOs.Payment;
using FluentValidation;

namespace Application.Validators.Payment
{
    public class InitiatePaymentDtoValidator : AbstractValidator<InitiatePaymentDto>
    {
        public InitiatePaymentDtoValidator()
        {
            RuleFor(x => x.PaymentMethodId)
                .GreaterThan(0).WithMessage("PaymentMethodId is required.");

            RuleFor(x => x.RedirectionUrls)
                .SetValidator(new EInvoiceRedirectionUrlsValidator()!)
                .When(x => x.RedirectionUrls is not null);
        }
    }
}
