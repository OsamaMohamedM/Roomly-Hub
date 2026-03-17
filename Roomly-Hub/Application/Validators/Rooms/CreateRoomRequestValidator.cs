using Application.DTOs;
using Domain.enums.Room;
using FluentValidation;

namespace Application.Validators
{
    public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequestDto>
    {
        public CreateRoomRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.RoomType)
                .NotEmpty().WithMessage("Room type is required.")
                .Must(value => Enum.TryParse<RoomType>(value, true, out _))
                .WithMessage("Invalid room type.");

            RuleFor(x => x.PricePerNight)
                .GreaterThan(0).WithMessage("Price per night must be greater than 0.");

            RuleFor(x => x.MaxGuests)
                .GreaterThan(0).WithMessage("Max guests must be greater than 0.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

            RuleFor(x => x.AddressLine)
                .NotEmpty().WithMessage("Address line is required.")
                .MaximumLength(300).WithMessage("Address line cannot exceed 300 characters.");

            RuleFor(x => x.CheckInTime)
                .NotEmpty().WithMessage("Check-in time is required.");

            RuleFor(x => x.CheckOutTime)
                .NotEmpty().WithMessage("Check-out time is required.")
                .GreaterThan(x => x.CheckInTime).WithMessage("Check-out time must be after check-in time.");
        }
    }
}
