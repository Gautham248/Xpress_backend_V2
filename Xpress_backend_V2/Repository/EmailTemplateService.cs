using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Microsoft.Extensions.Logging;

namespace Xpress_backend_V2.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(ILogger<EmailTemplateService> logger)
        {
            _logger = logger;
        }

        // GetSalutationDisplayName remains the same, using p.RecipientForSalutation and p.ProjectDetails
        private string GetSalutationDisplayName(EmailTemplateParameters p, string defaultName)
        {
            if (p.RecipientForSalutation != null && !string.IsNullOrWhiteSpace(p.RecipientForSalutation.EmployeeName) &&
                p.RecipientForSalutation.EmployeeName != "ManagerPlaceholder" && p.RecipientForSalutation.EmployeeName != "DUHeadPlaceholder")
            {
                return p.RecipientForSalutation.EmployeeName;
            }
            if (p.ProjectDetails != null)
            {
                // We still use ActualRecipientEmail here to determine if the fallback name should be PM or DUH name
                if (p.ActualRecipientEmail == p.ProjectDetails.ProjectManagerEmail && !string.IsNullOrWhiteSpace(p.ProjectDetails.ProjectManager))
                {
                    return p.ProjectDetails.ProjectManager;
                }
                if (p.ActualRecipientEmail == p.ProjectDetails.DuHeadEmail && !string.IsNullOrWhiteSpace(p.ProjectDetails.DuHeadName))
                {
                    return p.ProjectDetails.DuHeadName;
                }
            }
            return defaultName;
        }


        public Task<(string Subject, string HtmlBody)> GetManagerApprovalRequestEmailAsync(EmailTemplateParameters p)
        {
            // p.ActualRecipientEmail is still vital for AuditLogHandlerService to know WHO to send the email TO.
            // But it's NOT included in the action link itself.
            if (string.IsNullOrWhiteSpace(p.ActionBaseUrl))
            {
                _logger.LogError("GetManagerApprovalRequestEmailAsync: ActionBaseUrl missing for TR {ReqId}", p.TravelRequest.RequestId);
                return Task.FromResult(($"ERROR: Config Error TR {p.TravelRequest.RequestId}", "<p>Internal error: System link configuration missing.</p>"));
            }

            var subject = $"ACTION REQUIRED: Approve Travel Request {p.TravelRequest.RequestId} for {p.Requester.EmployeeName}";
            // MODIFIED: Links no longer include intendedActor. The confirmation page will get UserId from localStorage.
            var approveUrl = $"{p.ActionBaseUrl.TrimEnd('/')}/confirm-action.html?action=manager-approve&requestId={p.TravelRequest.RequestId}";
            var rejectUrl = $"{p.ActionBaseUrl.TrimEnd('/')}/confirm-action.html?action=manager-reject&requestId={p.TravelRequest.RequestId}";
            var salutationName = GetSalutationDisplayName(p, p.ProjectDetails.ProjectManager ?? "Manager");

            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {salutationName},</p>
                <p>Travel request ({p.TravelRequest.RequestId}) from {p.Requester.EmployeeName} ... requires your approval.</p>
                <p>Please click a link below to proceed (you may need to be logged into the system):</p>
                <p><a href='{approveUrl}' target='_blank' style='...'>Review & Approve Request</a></p>
                <p><a href='{rejectUrl}' target='_blank' style='...'>Review & Reject Request</a></p>
                <p>Thank you,<br/>Xpress Travel System</p>
                </body></html>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetDuHeadApprovalRequestEmailAsync(EmailTemplateParameters p)
        {
            if (string.IsNullOrWhiteSpace(p.ActionBaseUrl)) { /* ... */ }
            var subject = $"ACTION REQUIRED: DU Head Approval for TR {p.TravelRequest.RequestId}";
            var approveUrl = $"{p.ActionBaseUrl.TrimEnd('/')}/confirm-action.html?action=duhead-approve&requestId={p.TravelRequest.RequestId}";
            var rejectUrl = $"{p.ActionBaseUrl.TrimEnd('/')}/confirm-action.html?action=duhead-reject&requestId={p.TravelRequest.RequestId}";
            var salutationName = GetSalutationDisplayName(p, p.ProjectDetails.DuHeadName ?? "DU Head");
            // ... (construct body with these links) ...
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {salutationName},</p>
                <p>Travel request ({p.TravelRequest.RequestId}) ... requires your DU Head approval.</p>
                <p>Please click a link below to proceed (you may need to be logged into the system):</p>
                <p><a href='{approveUrl}' target='_blank' style='...'>Review & Approve (DU Head)</a></p>
                <p><a href='{rejectUrl}' target='_blank' style='...'>Review & Reject (DU Head)</a></p>
                <p>Thank you,<br/>Xpress Travel System</p>
                </body></html>";
            return Task.FromResult((subject, body));
        }

        public Task<(string Subject, string HtmlBody)> GetTicketOptionsForManagerEmailAsync(EmailTemplateParameters p)
        {
            if (string.IsNullOrWhiteSpace(p.ActualRecipientEmail) || p.TicketOptions == null || !p.TicketOptions.Any()) { /* ... */ }
            if (string.IsNullOrWhiteSpace(p.ActionBaseUrl)) { /* ... */ }
            var subject = $"ACTION REQUIRED: Select Ticket Option for TR {p.TravelRequest.RequestId}";
            var optionsHtml = new StringBuilder();
            foreach (var option in p.TicketOptions)
            {
                // MODIFIED: Link no longer includes intendedActor
                var selectUrl = $"{p.ActionBaseUrl.TrimEnd('/')}/confirm-action.html?action=select-ticket&requestId={p.TravelRequest.RequestId}&optionId={option.OptionId}";
                optionsHtml.Append($"<li style='margin-bottom: 10px;'>{option.Description} - <a href='{selectUrl}' target='_blank' style='...'>Review & Select Option</a></li>");
            }
            var salutationName = GetSalutationDisplayName(p, p.ProjectDetails.ProjectManager ?? "Manager");
            // ... (construct body with these links) ...
            var body = $@"
                 <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {salutationName},</p>
                <p>Ticket options are available for TR ({p.TravelRequest.RequestId})... Please select one:</p>
                <ul style='list-style-type: none; padding-left: 0;'>{optionsHtml.ToString()}</ul>
                <p>Thank you,<br/>Xpress Travel System</p>
                </body></html>";
            return Task.FromResult((subject, body));
        }

        // --- Informational emails like GetRequestSubmittedEmailAsync, GetInformAdminForTicketOptionsEmailAsync, etc. ---
        // --- remain unchanged as they don't typically generate these specific action links. ---
        // ... (pasted from your previous complete code) ...
        public Task<(string Subject, string HtmlBody)> GetRequestSubmittedEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Submitted by {p.Requester.EmployeeName}";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {GetSalutationDisplayName(p, p.Requester.EmployeeName)},</p>
                <p>Your travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' to {p.TravelRequest.DestinationPlace} has been submitted and is pending review.</p>
                <p>You will receive further notifications as your request progresses.</p>
                <p>Thank you,<br/>Xpress Travel System</p>
                </body></html>";
            return Task.FromResult((subject, body));
        }
        public Task<(string Subject, string HtmlBody)> GetInformAdminForTicketOptionsEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"ACTION REQUIRED: Provide Ticket Options for TR {p.TravelRequest.RequestId}";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi Admin Team,</p>
                <p>Travel request ({p.TravelRequest.RequestId}) for {p.Requester.EmployeeName} (Project: '{p.ProjectDetails.ProjectName}') has been fully approved.</p>
                <p>Please provide ticket options for this request.</p><p>Destination: {p.TravelRequest.DestinationPlace}</p>
                <p>Thank you,<br/>Xpress Travel System</p></body></html>";
            return Task.FromResult((subject, body));
        }
        public Task<(string Subject, string HtmlBody)> GetRequestApprovedEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Approved";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {GetSalutationDisplayName(p, p.Requester.EmployeeName)},</p>
                <p>Good news! Your travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' to {p.TravelRequest.DestinationPlace} has been approved by the DU Head.</p>
                <p>Admins will now work on providing ticket options.</p>
                <p>Thank you,<br/>Xpress Travel System</p></body></html>";
            return Task.FromResult((subject, body));
        }
        public Task<(string Subject, string HtmlBody)> GetRequestRejectedEmailAsync(EmailTemplateParameters p, string rejectedBy, string comments)
        {
            var subject = $"Travel Request {p.TravelRequest.RequestId} Rejected";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {GetSalutationDisplayName(p, p.Requester.EmployeeName)},</p>
                <p>We regret to inform you that travel request ({p.TravelRequest.RequestId}) for project '{p.ProjectDetails.ProjectName}' has been rejected by {rejectedBy}.</p>
                {(string.IsNullOrWhiteSpace(comments) ? "" : $"<p><strong>Comments:</strong> {comments}</p>")}
                <p>Thank you,<br/>Xpress Travel System</p></body></html>";
            return Task.FromResult((subject, body));
        }
        public Task<(string Subject, string HtmlBody)> GetTicketBookedEmailAsync(EmailTemplateParameters p)
        {
            var subject = $"Ticket Booked for Travel Request {p.TravelRequest.RequestId}";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {GetSalutationDisplayName(p, p.Requester.EmployeeName)},</p>
                <p>The ticket for travel request ({p.TravelRequest.RequestId}) to {p.TravelRequest.DestinationPlace} has been booked.</p>
                <p><strong>Selected Option:</strong> {p.SelectedOptionDescription ?? "Details to be provided by admin."}</p>
                <p>The admin team will share your itinerary and documents shortly.</p>
                <p>Thank you,<br/>Xpress Travel System</p></body></html>";
            return Task.FromResult((subject, body));
        }
        public Task<(string Subject, string HtmlBody)> GetGeneralNotificationEmailAsync(EmailTemplateParameters p, string notificationMessage)
        {
            var subject = $"Notification for Travel Request {p.TravelRequest.RequestId}";
            var body = $@"
                <!DOCTYPE html><html><body style='font-family: Arial, sans-serif;'>
                <p>Hi {GetSalutationDisplayName(p, "Team")},</p>
                <p>Notification for TR {p.TravelRequest.RequestId} ({p.Requester.EmployeeName}, Project: '{p.ProjectDetails.ProjectName}'):</p>
                <p><strong>Message:</strong> {notificationMessage}</p>
                <p>Thank you,<br/>Xpress Travel System</p></body></html>";
            return Task.FromResult((subject, body));
        }
    }
}
