using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequestDto>
    {
        public UpdateRoomRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.")
                .When(x => x.Title is not null);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description cannot be empty.")
                .When(x => x.Description is not null);

            RuleFor(x => x.PricePerNight)
                .GreaterThan(0).WithMessage("Price per night must be greater than 0.")
                .When(x => x.PricePerNight.HasValue);

            RuleFor(x => x.MaxGuests)
                .GreaterThan(0).WithMessage("Max guests must be greater than 0.")
                .When(x => x.MaxGuests.HasValue);

            RuleFor(x => x.CheckInTime)
                .Must(value => value is not null && value.Value != TimeSpan.Zero)
                .WithMessage("Check-in time is required.")
                .When(x => x.CheckInTime.HasValue);

            RuleFor(x => x.CheckOutTime)
                .Must(value => value is not null && value.Value != TimeSpan.Zero)
                .WithMessage("Check-out time is required.")
                .When(x => x.CheckOutTime.HasValue);

            RuleFor(x => x)
                .Must(x => !x.CheckInTime.HasValue || !x.CheckOutTime.HasValue || x.CheckOutTime > x.CheckInTime)
                .WithMessage("Check-out time must be after check-in time.");
        }
    }
}
