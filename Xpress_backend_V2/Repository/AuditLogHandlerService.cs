using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.Configuration;
using Xpress_backend_V2.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xpress_backend_V2.Services
{
    public class AuditLogHandlerService : IAuditLogHandlerService
    {
        private readonly ApiDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<AuditLogHandlerService> _logger;
        private readonly string _confirmationPageBaseUrl; 

        // Status ID constants
        private const int StatusPendingReview = 1;
        private const int StatusVerifiedByManager = 2;
        private const int StatusOptionsListedByAdmin = 3;
        private const int StatusOptionSelectedByManager = 4;
        private const int StatusDuApprovedByDuHead = 5;
        private const int StatusCancelled = 11;
        private const int StatusRejected = 12;

        public AuditLogHandlerService(
            ApiDbContext context,
            INotificationService notificationService,
            IEmailTemplateService emailTemplateService,
            IOptions<ApplicationSettings> appSettings, 
            ILogger<AuditLogHandlerService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailTemplateService = emailTemplateService;
            _logger = logger;


            _confirmationPageBaseUrl = appSettings.Value.BaseUrl;

            _logger.LogInformation("AuditLogHandlerService: _confirmationPageBaseUrl (for email links) initialized to: '{Url}'", _confirmationPageBaseUrl);
            if (string.IsNullOrWhiteSpace(_confirmationPageBaseUrl) || !_confirmationPageBaseUrl.StartsWith("http"))
            {
                _logger.LogError("CRITICAL CONFIGURATION ERROR: AuditLogHandlerService._confirmationPageBaseUrl is invalid or not configured in appsettings.json (ApplicationSettings:BaseUrl). It must be the full base URL of your frontend application (e.g., http://localhost:5173). Current value: '{Url}'", _confirmationPageBaseUrl);
            }
        }

        public async Task ProcessAuditLogEntryAsync(AuditLog auditLog)
        {
            Console.WriteLine(123);
            _logger.LogInformation("Processing AuditLog: ID={LID}, ReqID={RID}, Action={Act}, NewStatus={NSI}",
                auditLog.LogId, auditLog.RequestId, auditLog.ActionType, auditLog.NewStatusId);

            var travelRequest = await _context.TravelRequests
                .Include(tr => tr.User).Include(tr => tr.CurrentStatus).Include(tr => tr.SelectedTicketOption)
                .FirstOrDefaultAsync(tr => tr.RequestId == auditLog.RequestId);

            if (travelRequest == null) { _logger.LogError("TR not found for AuditLog {LID}. ReqId: {RID}", auditLog.LogId, auditLog.RequestId); return; }
            var requester = travelRequest.User;
            if (requester == null || string.IsNullOrWhiteSpace(requester.EmployeeEmail)) { _logger.LogError("Requester or Email missing for TR {RID}", travelRequest.RequestId); return; }

            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null) { _logger.LogError("RMT not found for ProjectCode {PC} (TR {RID})", travelRequest.ProjectCode, travelRequest.RequestId); return; }

            string managerActualEmail = projectDetails.ProjectManagerEmail;
            string duHeadActualEmail = projectDetails.DuHeadEmail;

            User managerForSalutation = !string.IsNullOrWhiteSpace(managerActualEmail)
     ? await _context.Users.FirstOrDefaultAsync(
         u => u.EmployeeEmail.ToLower() == managerActualEmail.ToLower() && u.IsActive)
     : null;

            User duHeadForSalutation = !string.IsNullOrWhiteSpace(duHeadActualEmail)
                ? await _context.Users.FirstOrDefaultAsync(
                    u => u.EmployeeEmail.ToLower() == duHeadActualEmail.ToLower() && u.IsActive)
                : null;

            var adminUsers = await _context.Users.Where(u => u.UserRole == "Admin" && u.IsActive).ToListAsync();
            var actorUser = auditLog.UserId > 0 ? await _context.Users.FindAsync(auditLog.UserId) : null;

            var baseEmailParams = new EmailTemplateParameters
            {
                TravelRequest = travelRequest,
                Requester = requester,
                ProjectDetails = projectDetails,
                ActionBaseUrl = _confirmationPageBaseUrl // Base URL for the frontend confirmation page links
            };
            _logger.LogInformation("BaseEmailParams created for TR {TRID}. ActionBaseUrl for templates: '{ABU}'", travelRequest.RequestId, baseEmailParams.ActionBaseUrl);


            switch (auditLog.NewStatusId)
            {
                case StatusPendingReview:
                    await HandleRequestSubmitted(baseEmailParams, managerActualEmail, managerForSalutation, duHeadActualEmail, duHeadForSalutation, adminUsers);
                    break;
                case StatusVerifiedByManager:
                    await HandleManagerApproved(baseEmailParams, duHeadActualEmail, duHeadForSalutation, adminUsers);
                    break;
                case StatusDuApprovedByDuHead:
                    await HandleDuHeadApproved(baseEmailParams, adminUsers, managerActualEmail, managerForSalutation);
                    break;
                case StatusOptionsListedByAdmin:
                    if (!string.IsNullOrWhiteSpace(managerActualEmail))
                        await HandleTicketOptionsSentToManager(baseEmailParams, managerActualEmail, managerForSalutation);
                    else
                        _logger.LogWarning("Manager email missing in RMT for TR {TRID} to send ticket options.", travelRequest.RequestId);
                    break;
                case StatusOptionSelectedByManager:
                    await HandleTicketBooked(baseEmailParams, managerActualEmail, managerForSalutation, duHeadActualEmail, duHeadForSalutation, adminUsers);
                    break;
                case StatusRejected:
                    string rejectedByRole = "Unknown"; string excludedEmail = null;
                    if (auditLog.ActionType == "ManagerRejected") { rejectedByRole = "Manager"; excludedEmail = managerActualEmail; }
                    else if (auditLog.ActionType == "DuHeadRejected") { rejectedByRole = "DU Head"; excludedEmail = duHeadActualEmail; }
                    else { _logger.LogWarning("Rejected status for TR {TRID} with unhandled ActionType: {Act}", travelRequest.RequestId, auditLog.ActionType); }
                    await HandleRequestRejected(baseEmailParams, rejectedByRole, auditLog.Comments, managerActualEmail, duHeadActualEmail, adminUsers, excludedEmail);
                    break;
                case StatusCancelled:
                    await HandleRequestCancelled(baseEmailParams, managerActualEmail, duHeadActualEmail, adminUsers, actorUser?.EmployeeName ?? "System");
                    break;
                default:
                    _logger.LogWarning("Unhandled NewStatusId {NSI} for TR {TRID}. ActionType: {Act}", auditLog.NewStatusId, travelRequest.RequestId, auditLog.ActionType);
                    break;
            }
        }

        private EmailTemplateParameters CreateEmailParams(
            EmailTemplateParameters baseP, User recipientUserForSalutation, string actualEmailForSendingAndLinks,
            List<TicketOptionInfoNoToken> ticketOpts = null, string selOptDesc = null, string message = null)
        {
            return new EmailTemplateParameters
            {
                TravelRequest = baseP.TravelRequest,
                Requester = baseP.Requester,
                ProjectDetails = baseP.ProjectDetails,
                ActionBaseUrl = baseP.ActionBaseUrl,
                RecipientForSalutation = recipientUserForSalutation,
                ActualRecipientEmail = actualEmailForSendingAndLinks,
                TicketOptions = ticketOpts,
                SelectedOptionDescription = selOptDesc,
                Message = message
            };
        }

        private async Task HandleRequestSubmitted(EmailTemplateParameters p, string managerActualEmail, User managerUserForSalutation, string duHeadActualEmail, User duHeadUserForSalutation, List<User> admins)
        {
            _logger.LogInformation("HandleRequestSubmitted for TR {TRID}. Notifying parties.", p.TravelRequest.RequestId);
            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetRequestSubmittedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p,
                    managerUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" }, // Uses RMT.ProjectManager for name
                    managerActualEmail);
                var (manSub, manBody) = await _emailTemplateService.GetManagerApprovalRequestEmailAsync(manEmailParams);
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }
            else { _logger.LogWarning("Manager email missing in RMT for TR {TRID}. Cannot send approval request.", p.TravelRequest.RequestId); }

            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p,
                    duHeadUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" }, // Uses RMT.DuHeadName
                    duHeadActualEmail);
                var (duSub, duBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(duEmailParams,
                    $"New travel request {p.TravelRequest.RequestId} by {p.Requester.EmployeeName} (Project: {p.ProjectDetails.ProjectName}) is pending manager approval.");
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }
            else { _logger.LogWarning("DUHead email missing in RMT for TR {TRID}.", p.TravelRequest.RequestId); }

            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, null, null);
                var (admSub, admBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(admEmailParams,
                    $"New travel request {p.TravelRequest.RequestId} by {p.Requester.EmployeeName} (Project: {p.ProjectDetails.ProjectName}) is pending manager approval.");
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
            else { _logger.LogInformation("No active admin users to notify for TR {TRID}.", p.TravelRequest.RequestId); }
        }

        private async Task HandleManagerApproved(EmailTemplateParameters p, string duHeadActualEmail, User duHeadUserForSalutation, List<User> admins)
        {
            _logger.LogInformation("HandleManagerApproved for TR {TRID}.", p.TravelRequest.RequestId);
            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(empEmailParams,
                $"Your request {p.TravelRequest.RequestId} was approved by manager. Pending DU Head approval.");
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p,
                    duHeadUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" },
                    duHeadActualEmail);
                var (duSub, duBody) = await _emailTemplateService.GetDuHeadApprovalRequestEmailAsync(duEmailParams);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }
            else { _logger.LogWarning("DU Head email missing for TR {TRID} after manager approval.", p.TravelRequest.RequestId); }

            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, null, null);
                var (admSub, admBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(admEmailParams,
                    $"Request {p.TravelRequest.RequestId} approved by manager. Pending DU Head approval.");
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }

        private async Task HandleDuHeadApproved(EmailTemplateParameters p, List<User> admins, string managerActualEmail, User managerUserForSalutation)
        {
            _logger.LogInformation("HandleDuHeadApproved for TR {TRID}.", p.TravelRequest.RequestId);
            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetRequestApprovedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p,
                    managerUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                    managerActualEmail);
                var (manSub, manBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(manEmailParams,
                    $"Request {p.TravelRequest.RequestId} for {p.Requester.EmployeeName} was approved by DU Head. Admin will provide ticket options.");
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }

            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, null, null);
                var (admSub, admBody) = await _emailTemplateService.GetInformAdminForTicketOptionsEmailAsync(admEmailParams);
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }
        private async Task HandleTicketOptionsSentToManager(EmailTemplateParameters p, string managerActualEmail, User managerUserForSalutation)
        {
            _logger.LogInformation("HandleTicketOptionsSentToManager for TR {TRID}.", p.TravelRequest.RequestId);
            var ticketOptionsFromDb = await _context.TicketOptions
                .Where(to => to.RequestId == p.TravelRequest.RequestId && !to.IsSelected)
                .OrderBy(to => to.CreatedAt).ToListAsync();
            if (!ticketOptionsFromDb.Any()) { _logger.LogWarning("No ticket options for TR {TRID}", p.TravelRequest.RequestId); return; }

            var optionInfos = ticketOptionsFromDb.Select(opt => new TicketOptionInfoNoToken { OptionId = opt.OptionId, Description = opt.OptionDescription }).ToList();
            var manEmailParams = CreateEmailParams(p,
                managerUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                managerActualEmail,
                ticketOpts: optionInfos);
            var (manSub, manBody) = await _emailTemplateService.GetTicketOptionsForManagerEmailAsync(manEmailParams);
            await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);

            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(empEmailParams,
                $"Ticket options for your request {p.TravelRequest.RequestId} sent to manager for selection.");
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);
        }

        private async Task HandleTicketBooked(EmailTemplateParameters p, string managerActualEmail, User managerUserForSalutation, string duHeadActualEmail, User duHeadUserForSalutation, List<User> admins)
        {
            _logger.LogInformation("HandleTicketBooked for TR {TRID}.", p.TravelRequest.RequestId);
            var selOptDesc = p.TravelRequest.SelectedTicketOption?.OptionDescription ?? "N/A";

            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail, selOptDesc: selOptDesc);
            var (empSub, empBody) = await _emailTemplateService.GetTicketBookedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p, managerUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" }, managerActualEmail, selOptDesc: selOptDesc);
                var (manSub, manBody) = await _emailTemplateService.GetTicketBookedEmailAsync(manEmailParams);
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p, duHeadUserForSalutation ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" }, duHeadActualEmail, selOptDesc: selOptDesc);
                var (duSub, duBody) = await _emailTemplateService.GetTicketBookedEmailAsync(duEmailParams);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }
            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, null, null, selOptDesc: selOptDesc);
                var (admSub, admBody) = await _emailTemplateService.GetTicketBookedEmailAsync(admEmailParams);
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }

        private async Task HandleRequestRejected(EmailTemplateParameters p, string rejectedByRole, string comments, string managerActualEmail, string duHeadActualEmail, List<User> admins, string excludedEmail)
        {
            _logger.LogInformation("HandleRequestRejected for TR {TRID} by {Role}.", p.TravelRequest.RequestId, rejectedByRole);
            var empEmailParams = CreateEmailParams(p, p.Requester, p.Requester.EmployeeEmail);
            var (empRejSub, empRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(empEmailParams, rejectedByRole, comments);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empRejSub, empRejBody);

            if (!string.IsNullOrWhiteSpace(managerActualEmail) && !managerActualEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
            {
                User managerUserObj = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail.Equals(managerActualEmail, StringComparison.OrdinalIgnoreCase) && u.IsActive);
                var manEmailParams = CreateEmailParams(p, managerUserObj ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" }, managerActualEmail);
                var (manRejSub, manRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(manEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(managerActualEmail, manRejSub, manRejBody);
            }
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail) && !duHeadActualEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
            {
                User duHeadUserObj = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail.Equals(duHeadActualEmail, StringComparison.OrdinalIgnoreCase) && u.IsActive);
                var duEmailParams = CreateEmailParams(p, duHeadUserObj ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" }, duHeadActualEmail);
                var (duRejSub, duRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(duEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duRejSub, duRejBody);
            }
            var adminEmails = admins.Where(a => !string.IsNullOrWhiteSpace(a.EmployeeEmail) && !a.EmployeeEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.EmployeeEmail).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, null, null);
                var (admRejSub, admRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(admEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(adminEmails, admRejSub, admRejBody);
            }
        }

        private async Task HandleRequestCancelled(EmailTemplateParameters p, string managerActualEmail, string duHeadActualEmail, List<User> admins, string cancelledBy)
        {
            _logger.LogInformation("HandleRequestCancelled for TR {TRID} by {Canceller}.", p.TravelRequest.RequestId, cancelledBy);
            var message = $"Travel request {p.TravelRequest.RequestId} has been cancelled by {cancelledBy}.";
            var recipientEmailsSource = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.Requester?.EmployeeEmail)) recipientEmailsSource.Add(p.Requester.EmployeeEmail);
            if (!string.IsNullOrWhiteSpace(managerActualEmail)) recipientEmailsSource.Add(managerActualEmail);
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail)) recipientEmailsSource.Add(duHeadActualEmail);
            recipientEmailsSource.AddRange(admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)));

            var distinctEmails = new HashSet<string>(recipientEmailsSource, StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctEmails.Any())
            {
                var cancelEmailParams = CreateEmailParams(p, null, null, message: message);
                var (sub, body) = await _emailTemplateService.GetGeneralNotificationEmailAsync(cancelEmailParams, message); // Pass message directly
                await _notificationService.SendEmailAsync(distinctEmails, sub, body);
            }
        }

    }
}