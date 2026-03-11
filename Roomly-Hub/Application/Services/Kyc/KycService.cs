using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using FluentValidation;

namespace Application.Services
{
    public class KycService : IKycService
    {
        private readonly IUserRepository _userRepository;
        private readonly IKycSubmissionRepository _kycSubmissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<SubmitKycRequestDto> _submitValidator;

        public KycService(
            IUserRepository userRepository,
            IKycSubmissionRepository kycSubmissionRepository,
            IUnitOfWork unitOfWork,
            IValidator<SubmitKycRequestDto> submitValidator)
        {
            _userRepository = userRepository;
            _kycSubmissionRepository = kycSubmissionRepository;
            _unitOfWork = unitOfWork;
            _submitValidator = submitValidator;
        }

        public async Task<Result<KycSubmissionResponseDto>> SubmitKycAsync(Guid userId, SubmitKycRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            if (userId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "User ID is required.");

            var validationResult = await _submitValidator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Request validation failed.", ValidationHelper.ToErrorDictionary(validationResult));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<KycSubmissionResponseDto>.Failure("USER_NOT_FOUND", "User not found.");

            if (!user.IsActive)
                return Result<KycSubmissionResponseDto>.Failure("ACCOUNT_INACTIVE", "Your account is inactive.");

            if (user.KycAttemptCount >= 3)
                return Result<KycSubmissionResponseDto>.Failure("KYC_LIMIT_REACHED", "Maximum KYC attempts reached.");

            var pendingSubmission = await _kycSubmissionRepository.GetPendingAsync(userId, cancellationToken);
            if (pendingSubmission is not null)
                return Result<KycSubmissionResponseDto>.Failure("PENDING_KYC_EXISTS", "You already have a pending KYC submission.");

            KycSubmission submission;
            try
            {
                submission = KycSubmission.Create(
                    user.Id,
                    requestDto.DocumentType,
                    $"{requestDto.DocumentType} Verification",
                    $"KYC submission for {requestDto.DocumentType}.",
                    requestDto.FrontImageUrl,
                    requestDto.SelfieUrl,
                    requestDto.BackImageUrl);

                user.AddKycSubmission(submission);
                user.SetKycPending();
            }
            catch (ArgumentException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            await _kycSubmissionRepository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<KycSubmissionResponseDto>.Success(Map(submission));
        }

        public async Task<Result<KycSubmissionResponseDto>> ReviewKycAsync(Guid reviewerId, ReviewKycRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Request body is required.");

            if (reviewerId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Reviewer ID is required.");

            if (requestDto.SubmissionId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Submission ID is required.");

            if (!requestDto.Approved && string.IsNullOrWhiteSpace(requestDto.RejectionReason))
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", "Rejection reason is required.");

            var reviewer = await _userRepository.GetByIdAsync(reviewerId, cancellationToken);
            if (reviewer is null)
                return Result<KycSubmissionResponseDto>.Failure("USER_NOT_FOUND", "Reviewer not found.");

            if (reviewer.AdminRole == AdminRole.None)
                return Result<KycSubmissionResponseDto>.Failure("FORBIDDEN", "Only admins can review KYC submissions.");

            var submission = await _kycSubmissionRepository.GetByIdAsync(requestDto.SubmissionId, cancellationToken);
            if (submission is null)
                return Result<KycSubmissionResponseDto>.Failure("SUBMISSION_NOT_FOUND", "KYC submission not found.");

            var user = await _userRepository.GetByIdAsync(submission.UserId, cancellationToken);
            if (user is null)
                return Result<KycSubmissionResponseDto>.Failure("USER_NOT_FOUND", "User not found.");

            try
            {
                if (requestDto.Approved)
                {
                    submission.Approve(reviewerId);
                    user.ApproveKyc();
                }
                else
                {
                    submission.Reject(reviewerId, requestDto.RejectionReason!);
                    user.RejectKyc();
                    user.IncrementKycAttempts();
                }
            }
            catch (ArgumentException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure("VALIDATION_ERROR", ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<KycSubmissionResponseDto>.Success(Map(submission));
        }

        private static KycSubmissionResponseDto Map(KycSubmission submission)
        {
            return new KycSubmissionResponseDto
            {
                Id = submission.Id,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt
            };
        }
    }
}
