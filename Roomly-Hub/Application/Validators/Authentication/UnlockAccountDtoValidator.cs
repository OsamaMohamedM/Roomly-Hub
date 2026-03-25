using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UnlockAccountDtoValidator : AbstractValidator<UnlockAccountDto>
    {
        public UnlockAccountDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");

            RuleFor(x => x.OtpCode)
                .NotEmpty().WithMessage("OTP code is required.")
                .Length(6).WithMessage("OTP code must be 6 digits.");
        }
    }
}
