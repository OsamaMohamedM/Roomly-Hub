using Application.DTOs.Notifications;
using FluentValidation;

namespace Application.Validators.Notifications
{
    public class NotificationDtoValidator : AbstractValidator<NotificationDto>
    {
        public NotificationDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ActionUrl).MaximumLength(500);
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Category).IsInEnum();
            RuleFor(x => x.Channel).IsInEnum();
            RuleFor(x => x.Status).IsInEnum();
        }
    }
}