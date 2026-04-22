using Application.DTOs.Auctions;
using FluentValidation;

namespace Application.Validators.Auctions
{
    public class PlaceBidRequestValidator : AbstractValidator<PlaceBidRequestDto>
    {
        public PlaceBidRequestValidator()
        {
            RuleFor(x => x.AuctionId)
                .NotEmpty();

            RuleFor(x => x.Amount)
                .GreaterThan(0);
        }
    }
}
