using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public class EmailTemplateParameters
    {
        public TravelRequest TravelRequest { get; set; }
        public User Requester { get; set; } // The employee who submitted the request
        public User RecipientForSalutation { get; set; } // The User object (actual or placeholder) for "Hi {Name}"
        public string ActualRecipientEmail { get; set; } // The definitive email string to send to AND use in action links
        public RMT ProjectDetails { get; set; }
        public string ActionBaseUrl { get; set; }

        public List<TicketOptionInfoNoToken> TicketOptions { get; set; }
        public string Message { get; set; }
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
