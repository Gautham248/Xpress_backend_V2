using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit.Text;
using MimeKit;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.Configuration;
using MailKit.Net.Smtp;

namespace Xpress_backend_V2.Repository
{
    public class NotificationService : INotificationService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IOptions<EmailSettings> emailSettings, ILogger<NotificationService> logger)
        {
            _logger = logger;

            if (emailSettings == null || emailSettings.Value == null)
            {
                _logger.LogCritical("EmailSettings are not configured in appsettings.json. The 'EmailSettings' section is missing or empty.");
                throw new InvalidOperationException("EmailSettings are not configured in appsettings.json.");
            }
            _emailSettings = emailSettings.Value;

            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpHost))
            {
                _logger.LogCritical("EmailSettings:SmtpHost is missing in appsettings.json.");
                throw new InvalidOperationException("EmailSettings:SmtpHost is not configured.");
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Attempted to send email with subject '{Subject}' but no recipient email was provided.", subject);
                return;
            }
            await SendEmailAsync(new List<string> { toEmail }, subject, htmlBody);
        }

        public async Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody)
        {
            var validEmails = toEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(System.StringComparer.OrdinalIgnoreCase).ToList();

            if (validEmails == null || !validEmails.Any())
            {
                _logger.LogWarning("No valid recipient emails provided for email with subject: {Subject}", subject);
                return;
            }

            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                foreach (var recipient in validEmails)
                {
                    emailMessage.To.Add(MailboxAddress.Parse(recipient));
                }

                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtpClient = new SmtpClient();
               

                await smtpClient.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                await smtpClient.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
                await smtpClient.SendAsync(emailMessage);
                await smtpClient.DisconnectAsync(true);
                _logger.LogInformation("Email sent to {Recipients} with subject: {Subject}", string.Join(",", validEmails), subject);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Recipients} with subject '{Subject}'. From: {FromEmail}, Host: {SmtpHost}, Port: {SmtpPort}, SSL: {EnableSsl}. Check SMTP settings and credentials.",
                    string.Join(",", validEmails), subject, _emailSettings.FromEmail, _emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.EnableSsl);

            }
        }
    }
}
