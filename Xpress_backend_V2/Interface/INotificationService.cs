using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody);
    }

}
