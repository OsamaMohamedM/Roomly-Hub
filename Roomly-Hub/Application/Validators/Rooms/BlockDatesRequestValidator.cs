using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class BlockDatesRequestValidator : AbstractValidator<BlockDatesRequestDto>
    {
        public BlockDatesRequestValidator()
        {
            RuleFor(x => x.From)
                .NotEmpty().WithMessage("From date is required.")
                .Must(from => from >= DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithMessage("From date cannot be in the past.");

            RuleFor(x => x.To)
                .NotEmpty().WithMessage("To date is required.")
                .GreaterThanOrEqualTo(x => x.From).WithMessage("To date must be greater than or equal to from date.");

            RuleFor(x => x)
                .Must(x => (x.To.DayNumber - x.From.DayNumber) <= 365)
                .WithMessage("Date range cannot exceed 365 days.");
        }
    }
}