using Application.DTOs.Wallet;
using Domain.enums.Booking;
using FluentValidation;

namespace Application.Validators.Wallet
{
    public class TopUpRequestValidator : AbstractValidator<TopUpRequestDto>
    {
        public TopUpRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero")
                .LessThanOrEqualTo(50000).WithMessage("Maximum top-up is 50,000 EGP");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .Must(method => Enum.TryParse<PaymentMethod>(method, true, out var parsed) && parsed != PaymentMethod.Wallet)
                .WithMessage("PaymentMethod is invalid for top-up");
        }
    }
}