using Application.Common.Constants;
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
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            if (userId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.UserIdRequired);

            var validationResult = await _submitValidator.ValidateAsync(requestDto, cancellationToken);
            if (!validationResult.IsValid)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed, ValidationHelper.ToErrorDictionary(validationResult));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

            if (!user.IsActive)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Kyc.AccountInactive, Errors.Messages.Kyc.AccountInactive);

            if (user.KycAttemptCount >= 3)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Kyc.KycLimitReached, Errors.Messages.Kyc.KycLimitReached);

            var pendingSubmission = await _kycSubmissionRepository.GetPendingAsync(userId, cancellationToken);
            if (pendingSubmission is not null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Kyc.PendingKycExists, Errors.Messages.Kyc.PendingKycExists);

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
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }

            await _kycSubmissionRepository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<KycSubmissionResponseDto>.Success(Map(submission));
        }

        public async Task<Result<KycSubmissionResponseDto>> ReviewKycAsync(Guid reviewerId, ReviewKycRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto is null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            if (reviewerId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.UserIdRequired);

            if (requestDto.SubmissionId == Guid.Empty)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.UserIdRequired);

            if (!requestDto.Approved && string.IsNullOrWhiteSpace(requestDto.RejectionReason))
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);

            var reviewer = await _userRepository.GetByIdAsync(reviewerId, cancellationToken);
            if (reviewer is null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

            if (reviewer.AdminRole == AdminRole.None)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Kyc.ForbiddenReview, Errors.Messages.Kyc.ForbiddenReview);

            var submission = await _kycSubmissionRepository.GetByIdAsync(requestDto.SubmissionId, cancellationToken);
            if (submission is null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Kyc.SubmissionNotFound, Errors.Messages.Kyc.SubmissionNotFound);

            var user = await _userRepository.GetByIdAsync(submission.UserId, cancellationToken);
            if (user is null)
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

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
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Result<KycSubmissionResponseDto>.Failure(Errors.Codes.Common.ValidationError, ex.Message);
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
