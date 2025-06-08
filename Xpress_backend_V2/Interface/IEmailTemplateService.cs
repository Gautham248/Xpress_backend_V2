using System.Collections.Generic;
using System.Threading.Tasks;
using Xpress_backend_V2.Models; // For User, TravelRequest, RMT

namespace Xpress_backend_V2.Interface
{
    public class EmailTemplateParameters
    {
        public TravelRequest TravelRequest { get; set; }
        public User Requester { get; set; }
        public User RecipientForSalutation { get; set; } // User obj for "Hi {Name}", can be placeholder
        public string ActualRecipientEmail { get; set; } // Definitive email string for 'To:' and for action links
        public RMT ProjectDetails { get; set; }
        public string ActionBaseUrl { get; set; } // Base URL of the CONFIRMATION PAGE (e.g., http://localhost:5173)

        public List<TicketOptionInfoNoToken> TicketOptions { get; set; }
        public string Message { get; set; } // For GetGeneralNotificationEmailAsync if it takes message via DTO
        public string SelectedOptionDescription { get; set; }
    }

    public class TicketOptionInfoNoToken
    {
        public string Description { get; set; }
        public int OptionId { get; set; }
    }

    public interface IEmailTemplateService
    {
        Task<(string Subject, string HtmlBody)> GetRequestSubmittedEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetManagerApprovalRequestEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetDuHeadApprovalRequestEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetInformAdminForTicketOptionsEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetTicketOptionsForManagerEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetRequestApprovedEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetRequestRejectedEmailAsync(EmailTemplateParameters parameters, string rejectedBy, string comments);
        Task<(string Subject, string HtmlBody)> GetTicketBookedEmailAsync(EmailTemplateParameters parameters);
        Task<(string Subject, string HtmlBody)> GetGeneralNotificationEmailAsync(EmailTemplateParameters parameters, string notificationMessage);
    }
}