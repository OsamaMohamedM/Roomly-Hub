using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class OtpVerifyDtoValidator : AbstractValidator<OtpVerifyDto>
    {
        public OtpVerifyDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.Otp)
                .NotEmpty().WithMessage("OTP is required.")
                .Length(6).WithMessage("OTP must be exactly 6 digits.")
                .Matches("^[0-9]{6}$").WithMessage("OTP must contain digits only.");
        }
    }
}