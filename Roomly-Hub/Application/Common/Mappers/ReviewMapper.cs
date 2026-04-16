using Application.DTOs.Reviews;
using Domain.Entities.Reviews;

namespace Application.Common.Mappers
{
    public interface IReviewMapper
    {
        ReviewResponseDto ToResponseDto(Review review, string reviewerName);
    }

    public class ReviewMapper : IReviewMapper
    {
        public ReviewResponseDto ToResponseDto(Review review, string reviewerName)
        {
            return new ReviewResponseDto
            {
                Id = review.Id,
                ReviewType = review.Type.ToString(),
                ReviewerName = reviewerName,
                Rating = review.Rating,
                Created = review.CreatedAt,
                Comment = review.Comment,
                Status = review.Status.ToString()
            };
        }
    }
}