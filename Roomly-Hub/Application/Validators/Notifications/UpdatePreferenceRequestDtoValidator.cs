using Application.DTOs.Notifications;
using FluentValidation;

namespace Application.Validators.Notifications
{
    public class UpdatePreferenceRequestDtoValidator : AbstractValidator<UpdatePreferenceRequestDto>
    {
        public UpdatePreferenceRequestDtoValidator()
        {
            RuleFor(x => x.Category).IsInEnum();
            RuleFor(x => x.Channel).IsInEnum();
        }
    }
}