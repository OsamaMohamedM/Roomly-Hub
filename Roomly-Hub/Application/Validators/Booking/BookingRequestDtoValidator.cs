using Application.DTOs.Booking;
using FluentValidation;

namespace Application.Validators.Booking
{
    public class BookingRequestDtoValidator : AbstractValidator<BookingRequestDto>
    {
        public BookingRequestDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

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

            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("Invalid payment method.");
        }
    }
}
