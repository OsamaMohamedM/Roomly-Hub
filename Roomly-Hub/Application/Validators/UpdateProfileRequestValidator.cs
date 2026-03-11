using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters long.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Bio)
                .MaximumLength(1000).WithMessage("Bio cannot exceed 1000 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

            RuleFor(x => x.ProfilePhotoUrl)
                .MaximumLength(500).WithMessage("Profile photo URL cannot exceed 500 characters.");
        }
    }
}
