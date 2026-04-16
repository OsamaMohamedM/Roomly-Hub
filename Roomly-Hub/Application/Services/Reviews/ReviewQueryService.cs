using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Reviews;
using Application.Interfaces.Services.Reviews;
using Domain.enums.Reviews;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Reviews
{
    public class ReviewQueryService : IReviewQueryService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewMapper _reviewMapper;
        private readonly ILogger<ReviewQueryService> _logger;

        public ReviewQueryService(
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            IReviewMapper reviewMapper,
            ILogger<ReviewQueryService> logger)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _reviewMapper = reviewMapper;
            _logger = logger;
        }

        public async Task<Result<List<ReviewResponseDto>>> GetRoomReviewsAsync(Guid roomId, CancellationToken ct)
        {
            _logger.LogInformation("Loading room reviews for room {RoomId}", roomId);
            var reviews = await _reviewRepository.GetByRoomIdAsync(roomId, ct);
            var visible = reviews.Where(r => r.Status == ReviewStatus.Visible).ToList();
            var dtos = await MapAsync(visible, ct);
            _logger.LogInformation("Loaded {Count} visible room reviews for room {RoomId}", dtos.Count, roomId);
            return Result<List<ReviewResponseDto>>.Success(dtos);
        }

        public async Task<Result<List<ReviewResponseDto>>> GetUserReviewsAsync(Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("Loading user reviews for user {UserId}", userId);
            var reviews = await _reviewRepository.GetBySubjectUserIdAsync(userId, ct);
            var visible = reviews.Where(r => r.Status == ReviewStatus.Visible).ToList();
            var dtos = await MapAsync(visible, ct);
            _logger.LogInformation("Loaded {Count} visible user reviews for user {UserId}", dtos.Count, userId);
            return Result<List<ReviewResponseDto>>.Success(dtos);
        }

        private async Task<List<ReviewResponseDto>> MapAsync(List<Domain.Entities.Reviews.Review> reviews, CancellationToken ct)
        {
            var result = new List<ReviewResponseDto>(reviews.Count);

            foreach (var review in reviews.OrderByDescending(r => r.CreatedAt))
            {
                var reviewer = await _userRepository.GetByIdAsync(review.ReviewerId, ct);
                result.Add(_reviewMapper.ToResponseDto(review, reviewer?.Name ?? string.Empty));
            }

            return result;
        }
    }
}