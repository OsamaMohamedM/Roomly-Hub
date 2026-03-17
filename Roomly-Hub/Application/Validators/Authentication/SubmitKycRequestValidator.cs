using Application.DTOs;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators
{
    public class SubmitKycRequestValidator : AbstractValidator<SubmitKycRequestDto>
    {
        public SubmitKycRequestValidator()
        {
            RuleFor(x => x.DocumentType)
                .IsInEnum().WithMessage("Invalid KYC document type.");

            RuleFor(x => x.FrontImageUrl)
                .NotEmpty().WithMessage("Front image URL is required.")
                .MaximumLength(500).WithMessage("Front image URL cannot exceed 500 characters.");

            RuleFor(x => x.SelfieUrl)
                .NotEmpty().WithMessage("Selfie URL is required.")
                .MaximumLength(500).WithMessage("Selfie URL cannot exceed 500 characters.");

            RuleFor(x => x.BackImageUrl)
                .MaximumLength(500).WithMessage("Back image URL cannot exceed 500 characters.");

            When(x => x.DocumentType == KycDocumentType.Passport, () =>
            {
                RuleFor(x => x.BackImageUrl)
                    .NotEmpty().WithMessage("Back image URL is required for passport.");
            });
        }
    }
}
