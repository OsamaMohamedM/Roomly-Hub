using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task<string> SendVerificationEmailAsync(User user);

        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}