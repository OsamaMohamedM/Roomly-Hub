using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Security.Cryptography;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private string GenerateSecureOtp(int length)
        {
            return RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, length)).ToString("D" + length);
        }

        public async Task<string> SendVerificationEmailAsync(User user)
        {
            var otp = GenerateSecureOtp(6);
            var subject = "Verify your email - [Brand Name]";
            var emailBody = $@"
                 <h1>Welcome, {user.Name}!</h1>
                  <p>Your verification code is: <b>{otp}</b></p>
                  <p>This code will expire in 25 minutes.</p>";
            await SendEmailAsync(user.Email.Value, subject, emailBody);
            return otp;
        }
    }
}