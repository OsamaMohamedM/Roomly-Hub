using Application.DTOs.Payment;
using FluentValidation;

namespace Application.Validators.Payment
{
    public class WebHookModelValidator : AbstractValidator<WebHookModel>
    {
        public WebHookModelValidator()
        {
            RuleFor(x => x.InvoiceId)
                .GreaterThan(0).WithMessage("InvoiceId is required.");

            RuleFor(x => x.InvoiceKey)
                .NotEmpty().WithMessage("InvoiceKey is required.");

            RuleFor(x => x.HashKey)
                .NotEmpty().WithMessage("HashKey is required.");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("PaymentMethod is required.");

            RuleFor(x => x.InvoiceStatus)
                .NotEmpty().WithMessage("InvoiceStatus is required.");
        }
    }
}
