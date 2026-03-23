using Application.DTOs.Booking;
using FluentValidation;

namespace Application.Validators.Booking
{
    public class BookingCancelRequestDtoValidator : AbstractValidator<BookingCancelRequestDto>
    {
        public BookingCancelRequestDtoValidator()
        {
            RuleFor(x => x.BookingId)
                .NotEmpty().WithMessage("BookingId is required.");

            RuleFor(x => x.GuestId)
                .NotNull().WithMessage("GuestId is required.")
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("GuestId is invalid.");
        }
    }
}
