using Application.Common.Constants;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Reviews;
using Application.Events.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services.Reviews;
using Domain.Entities.Reviews;
using Domain.Enums;
using Domain.enums.Booking;
using Domain.enums.Reviews;
using Domain.enums.Room;
using Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Services.Reviews
{
    public class ReviewCommandService : IReviewCommandService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<SubmitReviewRequestDto> _submitReviewValidator;
        private readonly IReviewMapper _reviewMapper;
        private readonly IPublisher _publisher;
        private readonly ILogger<ReviewCommandService> _logger;

        public ReviewCommandService(
            IBookingRepository bookingRepository,
            IRoomRepository roomRepository,
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IValidator<SubmitReviewRequestDto> submitReviewValidator,
            IReviewMapper reviewMapper,
            IPublisher publisher,
            ILogger<ReviewCommandService> logger)
        {
            _bookingRepository = bookingRepository;
            _roomRepository = roomRepository;
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _submitReviewValidator = submitReviewValidator;
            _reviewMapper = reviewMapper;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<ReviewResponseDto>> SubmitReviewAsync(Guid reviewerId, SubmitReviewRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Submitting review for booking {BookingId} by reviewer {ReviewerId}", dto?.BookingId, reviewerId);

            if (dto == null || reviewerId == Guid.Empty)
            {
                _logger.LogWarning("Submit review failed due to invalid payload or reviewer id. ReviewerId: {ReviewerId}", reviewerId);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);
            }

            var validationResult = await _submitReviewValidator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Submit review validation failed for booking {BookingId} by reviewer {ReviewerId}", dto.BookingId, reviewerId);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            if (!Enum.TryParse<ReviewType>(dto.ReviewType, true, out var reviewType))
            {
                _logger.LogWarning("Submit review failed due to invalid review type {ReviewType} for booking {BookingId}", dto.ReviewType, dto.BookingId);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.InvalidReviewType, Errors.Messages.Review.InvalidReviewType);
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(dto.BookingId, ct);
            if (booking == null)
            {
                _logger.LogWarning("Submit review failed. Booking {BookingId} not found", dto.BookingId);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.Status != BookingStatus.Completed)
            {
                _logger.LogWarning("Submit review failed. Booking {BookingId} is not completed", booking.Id);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.BookingNotCompleted, Errors.Messages.Review.BookingNotCompleted);
            }

            if (reviewType == ReviewType.RoommateToGuest && booking.Room.RoomType != RoomType.SharedRoom)
            {
                _logger.LogWarning("Submit review failed. Roommate review requested for non-shared room {RoomId}", booking.RoomId);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.SharedRoomRequired, Errors.Messages.Review.SharedRoomRequired);
            }

            var reviewerAllowed = await IsReviewerAllowedAsync(booking, reviewerId, reviewType, dto.SubjectId, ct);
            if (!reviewerAllowed)
            {
                _logger.LogWarning("Submit review forbidden. Reviewer {ReviewerId}, Booking {BookingId}, ReviewType {ReviewType}", reviewerId, booking.Id, reviewType);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.ForbiddenReviewAction, Errors.Messages.Review.ForbiddenReviewAction);
            }

            var duplicateExists = await _reviewRepository.ExistsAsync(dto.BookingId, reviewerId, reviewType, ct);
            if (duplicateExists)
            {
                _logger.LogWarning("Submit review failed due to duplicate review. Reviewer {ReviewerId}, Booking {BookingId}, ReviewType {ReviewType}", reviewerId, booking.Id, reviewType);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.AlreadySubmitted, Errors.Messages.Review.AlreadySubmitted);
            }

            var subjectUserId = ResolveSubjectUserId(booking, reviewType, dto.SubjectId);
            if (!subjectUserId.HasValue)
            {
                _logger.LogWarning("Submit review failed due to invalid subject. Reviewer {ReviewerId}, Booking {BookingId}, ReviewType {ReviewType}", reviewerId, booking.Id, reviewType);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Review.InvalidSubject, Errors.Messages.Review.InvalidSubject);
            }

            Review review;
            try
            {
                review = Review.Create(
                    reviewerId,
                    subjectUserId,
                    booking.RoomId,
                    booking.Id,
                    dto.Rating,
                    dto.Comment?.Trim() ?? string.Empty,
                    reviewType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Submit review failed due to domain validation. Reviewer {ReviewerId}, Booking {BookingId}", reviewerId, booking.Id);
                return Result<ReviewResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }

            await _reviewRepository.AddAsync(review, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            if (reviewType == ReviewType.GuestToRoom)
            {
                await RecalculateRoomRatingAsync(booking.RoomId, ct);
            }

            try
            {
                if (subjectUserId.HasValue)
                {
                    await _publisher.Publish(new ReviewReceivedEvent(review.Id, booking.Id, subjectUserId.Value, reviewerId), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish ReviewReceivedEvent for review {ReviewId}", review.Id);
            }

            var reviewer = await _userRepository.GetByIdAsync(reviewerId, ct);
            var dtoResponse = _reviewMapper.ToResponseDto(review, reviewer?.Name ?? string.Empty);

            _logger.LogInformation(
                "Review submitted. ReviewId: {ReviewId}, ReviewerId: {ReviewerId}, ReviewType: {ReviewType}, BookingId: {BookingId}",
                review.Id,
                reviewerId,
                reviewType,
                booking.Id);

            return Result<ReviewResponseDto>.Success(dtoResponse);
        }

        public async Task<Result> FlagReviewAsync(Guid moderatorId, Guid reviewId, FlagReviewRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Flag review requested. ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", reviewId, moderatorId);

            var moderator = await _userRepository.GetByIdAsync(moderatorId, ct);
            if (moderator == null || (moderator.AdminRole != AdminRole.Moderator && moderator.AdminRole != AdminRole.SuperAdmin))
            {
                _logger.LogWarning("Flag review forbidden. ModeratorId: {ModeratorId}", moderatorId);
                return Result.Failure(Errors.Codes.Review.ForbiddenReviewAction, Errors.Messages.Review.ForbiddenReviewAction);
            }

            var review = await _reviewRepository.GetByIdAsync(reviewId, ct);
            if (review == null)
            {
                _logger.LogWarning("Flag review failed. Review {ReviewId} not found", reviewId);
                return Result.Failure(Errors.Codes.Review.ReviewNotFound, Errors.Messages.Review.ReviewNotFound);
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                _logger.LogWarning("Flag review failed due to missing reason. ReviewId: {ReviewId}", reviewId);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            review.Flag(dto.Reason.Trim());
            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Review flagged successfully. ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", reviewId, moderatorId);
            return Result.Success();
        }

        public async Task<Result> RemoveReviewAsync(Guid moderatorId, Guid reviewId, CancellationToken ct)
        {
            _logger.LogInformation("Remove review requested. ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", reviewId, moderatorId);

            var moderator = await _userRepository.GetByIdAsync(moderatorId, ct);
            if (moderator == null || (moderator.AdminRole != AdminRole.Moderator && moderator.AdminRole != AdminRole.SuperAdmin))
            {
                _logger.LogWarning("Remove review forbidden. ModeratorId: {ModeratorId}", moderatorId);
                return Result.Failure(Errors.Codes.Review.ForbiddenReviewAction, Errors.Messages.Review.ForbiddenReviewAction);
            }

            var review = await _reviewRepository.GetByIdAsync(reviewId, ct);
            if (review == null)
            {
                _logger.LogWarning("Remove review failed. Review {ReviewId} not found", reviewId);
                return Result.Failure(Errors.Codes.Review.ReviewNotFound, Errors.Messages.Review.ReviewNotFound);
            }

            review.Remove();
            _reviewRepository.Update(review);
            await _unitOfWork.SaveChangesAsync(ct);

            if (review.Type == ReviewType.GuestToRoom && review.SubjectRoomId.HasValue)
            {
                await RecalculateRoomRatingAsync(review.SubjectRoomId.Value, ct);
            }

            _logger.LogInformation("Review removed successfully. ReviewId: {ReviewId}, ModeratorId: {ModeratorId}", reviewId, moderatorId);
            return Result.Success();
        }

        private async Task<bool> IsReviewerAllowedAsync(Domain.Entities.Booking.Booking booking, Guid reviewerId, ReviewType reviewType, Guid? subjectUserId, CancellationToken ct)
        {
            return reviewType switch
            {
                ReviewType.GuestToRoom => booking.GuestId == reviewerId,
                ReviewType.GuestToHost => booking.GuestId == reviewerId,
                ReviewType.HostToGuest => booking.Room.HostId == reviewerId,
                ReviewType.RoommateToGuest => await IsValidRoommateReviewAsync(booking, reviewerId, subjectUserId, ct),
                _ => false
            };
        }

        private async Task<bool> IsValidRoommateReviewAsync(Domain.Entities.Booking.Booking booking, Guid reviewerId, Guid? subjectUserId, CancellationToken ct)
        {
            if (!subjectUserId.HasValue || subjectUserId.Value == reviewerId)
            {
                return false;
            }

            var overlapping = await _bookingRepository.GetOverlappingBookingsForRoomAsync(
                booking.RoomId,
                booking.CheckInDate,
                booking.CheckOutDate,
                null,
                ct);

            var reviewerIsRoommateGuest = overlapping.Any(b => b.GuestId == reviewerId && b.Id != booking.Id);
            var subjectIsRoommateGuest = overlapping.Any(b => b.GuestId == subjectUserId.Value);
            return reviewerIsRoommateGuest && subjectIsRoommateGuest;
        }

        private Guid? ResolveSubjectUserId(Domain.Entities.Booking.Booking booking, ReviewType reviewType, Guid? requestedSubjectUserId)
        {
            return reviewType switch
            {
                ReviewType.GuestToRoom => booking.Room.HostId,
                ReviewType.GuestToHost => booking.Room.HostId,
                ReviewType.HostToGuest => booking.GuestId,
                ReviewType.RoommateToGuest => requestedSubjectUserId,
                _ => null
            };
        }

        private async Task RecalculateRoomRatingAsync(Guid roomId, CancellationToken ct)
        {
            _logger.LogInformation("Recalculating room rating for room {RoomId}", roomId);

            var room = await _roomRepository.GetByIdAsync(roomId, ct);
            if (room == null)
            {
                _logger.LogWarning("Room rating recalculation skipped. Room {RoomId} not found", roomId);
                return;
            }

            var reviews = await _reviewRepository.GetByRoomIdAsync(roomId, ct);
            var visibleGuestToRoom = reviews
                .Where(r => r.Status == ReviewStatus.Visible && r.Type == ReviewType.GuestToRoom)
                .ToList();

            if (visibleGuestToRoom.Count == 0)
            {
                room.UpdateAverageRating(0);
                await _roomRepository.UpdateAsync(room, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("Room rating reset for room {RoomId} due to no visible guest-to-room reviews", roomId);
                return;
            }

            var average = Math.Round(visibleGuestToRoom.Average(r => r.Rating), 2, MidpointRounding.AwayFromZero);
            room.UpdateAverageRating((decimal)average);
            await _roomRepository.UpdateAsync(room, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Room rating recalculated for room {RoomId}. New average: {Average}", roomId, average);
        }
    }
}