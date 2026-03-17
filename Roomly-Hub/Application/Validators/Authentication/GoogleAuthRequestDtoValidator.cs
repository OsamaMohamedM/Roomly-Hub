using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class GoogleAuthRequestDtoValidator : AbstractValidator<GoogleAuthRequestDto>
    {
        public GoogleAuthRequestDtoValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty().WithMessage("Google ID token is required.");
        }
    }
}
