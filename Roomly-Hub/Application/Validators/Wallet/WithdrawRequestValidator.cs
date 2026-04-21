using Application.DTOs.Wallet;
using FluentValidation;

namespace Application.Validators.Wallet
{
    public class WithdrawRequestValidator : AbstractValidator<WithdrawRequestDto>
    {
        public WithdrawRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0);
        }
    }
}