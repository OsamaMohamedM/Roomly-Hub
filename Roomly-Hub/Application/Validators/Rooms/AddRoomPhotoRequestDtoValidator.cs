using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class AddRoomPhotoRequestDtoValidator : AbstractValidator<AddRoomPhotoRequestDto>
    {
        public AddRoomPhotoRequestDtoValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Photo URL is required.")
                .MaximumLength(500).WithMessage("Photo URL cannot exceed 500 characters.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Photo URL must be a valid absolute URL.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
