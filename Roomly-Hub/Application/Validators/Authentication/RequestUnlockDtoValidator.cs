using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class RequestUnlockDtoValidator : AbstractValidator<RequestUnlockDto>
    {
        public RequestUnlockDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
        }
    }
}
