using Application.DTOs.Auctions;
using Domain.enums.Auction;
using FluentValidation;

namespace Application.Validators.Auctions
{
    public class CreateAuctionRequestValidator : AbstractValidator<CreateAuctionRequestDto>
    {
        public CreateAuctionRequestValidator()
        {
            RuleFor(x => x.RoomId)
                .NotEmpty();

            RuleFor(x => x.CheckInDate)
                .NotEqual(default(DateOnly))
                .Must(date => date > DateOnly.FromDateTime(DateTime.UtcNow));

            RuleFor(x => x.CheckOutDate)
                .NotEqual(default(DateOnly))
                .Must((dto, checkOut) => checkOut > dto.CheckInDate);

            RuleFor(x => x.StartingPrice)
                .GreaterThan(0);

            RuleFor(x => x.MinBidIncrementType)
                .NotEmpty()
                .Must(value => Enum.TryParse<IncrementType>(value, true, out _));

            RuleFor(x => x.MinBidIncrementValue)
                .GreaterThan(0)
                .Must((dto, value) =>
                {
                    if (!Enum.TryParse<IncrementType>(dto.MinBidIncrementType, true, out var type))
                        return true;
                    if (type != IncrementType.Percentage)
                        return true;
                    return value < 1;
                });

            RuleFor(x => x.Duration)
                .NotEmpty()
                .Must(value => Enum.TryParse<AuctionDuration>(value, true, out _));
        }
    }
}
