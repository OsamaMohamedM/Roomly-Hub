using Application.DTOs.Booking;
using FluentValidation;

namespace Application.Validators.Booking
{
    public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingDtoValidator()
        {
            RuleFor(x => x.RoomId)
                .NotEmpty().WithMessage("RoomId is required.");

            RuleFor(x => x.StartDate)
                .Must(d => d != default)
                .WithMessage("StartDate is required.");

            RuleFor(x => x.EndDate)
                .Must(d => d != default)
                .WithMessage("EndDate is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("EndDate must be after StartDate.");
        }
    }
}
