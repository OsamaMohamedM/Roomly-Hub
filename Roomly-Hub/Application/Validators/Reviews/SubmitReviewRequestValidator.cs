using Application.DTOs.Reviews;
using FluentValidation;

namespace Application.Validators.Reviews
{
    public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequestDto>
    {
        public SubmitReviewRequestValidator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("BookingId is required.");
            RuleFor(x => x.ReviewType).NotEmpty().WithMessage("ReviewType is required.");
            RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
            RuleFor(x => x.Comment).MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters.");
            RuleFor(x => x.SubjectId).NotEmpty().When(x => x.ReviewType == "User").WithMessage("SubjectId is required for User reviews.");
            RuleFor(x => x.SubjectRoomId).NotEmpty().When(x => x.ReviewType == "Room").WithMessage("SubjectRoomId is required for Room reviews.");
        }
    }
}