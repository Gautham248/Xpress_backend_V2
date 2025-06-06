using Microsoft.Extensions.Options;
using System.Text;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.Configuration;

namespace Xpress_backend_V2.Repository
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public EmailTemplateService() { }

        private string GetRecipientNameForSalutation(User recipientUser, string defaultNameIfUserNull)
        {
            // Use recipientUser.EmployeeName if available, otherwise fallback to RMT name or generic default
            if (recipientUser != null && !string.IsNullOrWhiteSpace(recipientUser.EmployeeName) && recipientUser.EmployeeName != "ManagerPlaceholder" && recipientUser.EmployeeName != "DUHeadPlaceholder")
            {
                return recipientUser.EmployeeName;
            }
            return defaultNameIfUserNull; // This defaultName could be from RMT.ProjectManagerName or "Manager"
        }


        public Task<(string Subject, string HtmlBody)> GetRequestSubmittedEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Submitted by {p.Requester.EmployeeName}";
            // For this email, p.RecipientForSalutation is the Requester themselves
            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, p.Requester.EmployeeName)},</p>
                <p>Your travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' to {p.TravelRequest.DestinationPlace} has been submitted and is pending review.</p>
                <p>You will receive further notifications as your request progresses.</p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetManagerApprovalRequestEmailAsync(EmailTemplateParameters p)
        {
            if (string.IsNullOrWhiteSpace(p.ActualRecipientEmail)) // This is the manager's email from RMT
            {
                return Task.FromResult(($"ERROR: Missing Manager Email for TR {p.TravelRequest.RequestId}", "<p>Could not generate manager approval email: manager's email address is missing.</p>"));
            }

            var subject = $"ACTION REQUIRED: Approve Travel Request {p.TravelRequest.RequestId} for {p.Requester.EmployeeName}";
            var approveUrl = $"{p.ActionBaseUrl}/manager-approve?requestId={p.TravelRequest.RequestId}&actorEmail={Uri.EscapeDataString(p.ActualRecipientEmail)}";
            var rejectUrl = $"{p.ActionBaseUrl}/manager-reject?requestId={p.TravelRequest.RequestId}&actorEmail={Uri.EscapeDataString(p.ActualRecipientEmail)}";

            var managerName = p.ProjectDetails.ProjectManager ?? "Manager"; // Use RMT name or default

            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, managerName)},</p>
                <p>Travel request ({p.TravelRequest.RequestId}) from {p.Requester.EmployeeName} for project '{p.ProjectDetails.ProjectName}' (Destination: {p.TravelRequest.DestinationPlace}) requires your approval.</p>
                <p><strong>Purpose:</strong> {p.TravelRequest.PurposeOfTravel}</p>
                <p>Please take action:</p>
                <p><a href='{approveUrl}' style='display: inline-block; padding: 10px 15px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Approve Request</a></p>
                <p><a href='{rejectUrl}' style='display: inline-block; padding: 10px 15px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin-left: 10px;'>Reject Request</a></p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetDuHeadApprovalRequestEmailAsync(EmailTemplateParameters p)
        {
            if (string.IsNullOrWhiteSpace(p.ActualRecipientEmail)) // This is the DU Head's email from RMT
            {
                return Task.FromResult(($"ERROR: Missing DU Head Email for TR {p.TravelRequest.RequestId}", "<p>Could not generate DU Head approval email: DU Head's email address is missing.</p>"));
            }

            var subject = $"ACTION REQUIRED: DU Head Approval for TR {p.TravelRequest.RequestId}";
            var approveUrl = $"{p.ActionBaseUrl}/duhead-approve?requestId={p.TravelRequest.RequestId}&actorEmail={Uri.EscapeDataString(p.ActualRecipientEmail)}";
            var rejectUrl = $"{p.ActionBaseUrl}/duhead-reject?requestId={p.TravelRequest.RequestId}&actorEmail={Uri.EscapeDataString(p.ActualRecipientEmail)}";

            var duHeadName = p.ProjectDetails.DuHeadName ?? "DU Head"; // Use RMT name or default

            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, duHeadName)},</p>
                <p>Travel request ({p.TravelRequest.RequestId}) for {p.Requester.EmployeeName} (Project: '{p.ProjectDetails.ProjectName}') has been approved by the manager and now requires your DU Head approval.</p>
                <p>Please take action:</p>
                <p><a href='{approveUrl}' style='display: inline-block; padding: 10px 15px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Approve (DU Head)</a></p>
                <p><a href='{rejectUrl}' style='display: inline-block; padding: 10px 15px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin-left: 10px;'>Reject (DU Head)</a></p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetInformAdminForTicketOptionsEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"ACTION REQUIRED: Provide Ticket Options for TR {p.TravelRequest.RequestId}";
            var body = $@"
                <p>Hi Admin Team,</p>
                <p>Travel request ({p.TravelRequest.RequestId}) for {p.Requester.EmployeeName} (Project: '{p.ProjectDetails.ProjectName}') has been fully approved.</p>
                <p>Please provide ticket options for this request via the admin portal/system.</p>
                <p><strong>Destination:</strong> {p.TravelRequest.DestinationPlace}</p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetTicketOptionsForManagerEmailAsync(EmailTemplateParameters p)
        {
            if (string.IsNullOrWhiteSpace(p.ActualRecipientEmail) || p.TicketOptions == null || !p.TicketOptions.Any())
            {
                return Task.FromResult(($"ERROR: Missing Manager Email or Options for TR {p.TravelRequest.RequestId}", "<p>Could not generate ticket options email: Recipient or options missing.</p>"));
            }
            var subject = $"ACTION REQUIRED: Select Ticket Option for TR {p.TravelRequest.RequestId}";
            var optionsHtml = new StringBuilder();
            foreach (var option in p.TicketOptions)
            {
                var selectUrl = $"{p.ActionBaseUrl}/select-ticket?requestId={p.TravelRequest.RequestId}&actorEmail={Uri.EscapeDataString(p.ActualRecipientEmail)}&optionId={option.OptionId}";
                optionsHtml.Append($"<li style='margin-bottom: 10px;'>{option.Description} - <a href='{selectUrl}' style='padding:5px 8px; background-color:#007bff; color:white; text-decoration:none; border-radius:3px;'>Select This Option</a></li>");
            }

            var managerName = p.ProjectDetails.ProjectManager ?? "Manager";

            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, managerName)},</p>
                <p>Ticket options are available for travel request ({p.TravelRequest.RequestId}) for {p.Requester.EmployeeName}. Please select one:</p>
                <ul style='list-style-type: none; padding-left: 0;'>{optionsHtml.ToString()}</ul>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetRequestApprovedEmailAsync(EmailTemplateParameters p) // General approval to employee
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Approved";
            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, p.Requester.EmployeeName)},</p>
                <p>Good news! Your travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' to {p.TravelRequest.DestinationPlace} has been approved by DU Head.</p>
                <p>Admins will now work on providing ticket options.</p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetRequestRejectedEmailAsync(EmailTemplateParameters p, string rejectedBy, string comments)
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Rejected";
            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, p.Requester.EmployeeName)},</p> 
                <p>We regret to inform you that travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' has been rejected by {rejectedBy}.</p>
                {(string.IsNullOrWhiteSpace(comments) ? "" : $"<p><strong>Comments:</strong> {comments}</p>")}
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetTicketBookedEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"Ticket Booked for Travel Request {p.TravelRequest.RequestId}";
            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, p.Requester.EmployeeName)},</p> 
                <p>The ticket for travel request ({p.TravelRequest.RequestId}) to {p.TravelRequest.DestinationPlace} has been booked.</p>
                <p><strong>Selected Option:</strong> {p.SelectedOptionDescription ?? "Details will be provided by the admin team."}</p>
                <p>The admin team will share your itinerary and any other relevant documents shortly if applicable.</p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetGeneralNotificationEmailAsync(EmailTemplateParameters p, string notificationMessage)
        {
            var subject = $"Notification for Travel Request {p.TravelRequest.RequestId}";
            // p.RecipientForSalutation might be null for a general admin email, or could be a specific person
            var body = $@"
                <p>Hi {GetRecipientNameForSalutation(p.RecipientForSalutation, "Team")},</p>
                <p>This is a notification regarding travel request {p.TravelRequest.RequestId} for {p.Requester.EmployeeName} (Project: '{p.ProjectDetails.ProjectName}').</p>
                <p><strong>Message:</strong> {notificationMessage}</p>
                <p>Thank you,<br/>Xpress Travel System</p>";
            return Task.FromResult((subject, body));
        }
    }
}
