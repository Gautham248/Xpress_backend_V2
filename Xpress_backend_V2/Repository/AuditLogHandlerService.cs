using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.Configuration;
using Xpress_backend_V2.Interface; // Make sure EmailTemplateParameters is accessible here
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
        private readonly ApplicationSettings _appSettings;

        // Status ID constants from your RequestStatuses table
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
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        public async Task ProcessAuditLogEntryAsync(AuditLog auditLog)
        {
            _logger.LogInformation(
                "Processing AuditLogId: {LogId} for RequestId: {ReqId}, Action: {Action}, NewStatus: {NewStatus}",
                auditLog.LogId, auditLog.RequestId, auditLog.ActionType, auditLog.NewStatusId);

            // 1. Fetch TravelRequest using RequestId from AuditLog
            var travelRequest = await _context.TravelRequests
                .Include(tr => tr.User) // To get Employee details
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.SelectedTicketOption)
                .FirstOrDefaultAsync(tr => tr.RequestId == auditLog.RequestId);

            if (travelRequest == null)
            {
                _logger.LogError("TR not found for AuditLogId {AuditLogId}.", auditLog.LogId, auditLog.RequestId);
                return;
            }

            // 2. Fetch Employee (Requester) from TravelRequest
            var requester = travelRequest.User;
            if (requester == null || string.IsNullOrWhiteSpace(requester.EmployeeEmail))
            {
                _logger.LogError("Requester or RequesterEmail missing for TR {RequestId}.", travelRequest.RequestId);
                return; // Cannot send employee notifications
            }

            // 3. Fetch RMT details using ProjectCode from TravelRequest
            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null)
            {
                _logger.LogError("RMT not found for ProjectCode {ProjectCode} (TR {RequestId}).",
                    travelRequest.ProjectCode, travelRequest.RequestId);
                return; // Cannot get Manager/DU Head emails
            }

            // 4. Get Manager & DU Head emails directly from RMT (projectDetails)
            string managerActualEmail = projectDetails.ProjectManagerEmail;
            string duHeadActualEmail = projectDetails.DuHeadEmail;

            // 5. Optionally, try to find User objects for manager/DU Head for better salutations
            //    These User objects are for display/template purposes; the actual sending uses the email strings above.
            User managerAsUserForTemplate = !string.IsNullOrWhiteSpace(managerActualEmail) ?
                await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == managerActualEmail && u.IsActive)
                : null;
            User duHeadAsUserForTemplate = !string.IsNullOrWhiteSpace(duHeadActualEmail) ?
                await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == duHeadActualEmail && u.IsActive)
                : null;

            // Fetch other necessary users
            var adminUsers = await _context.Users.Where(u => u.UserRole == "Admin" && u.IsActive).ToListAsync();
            var actorUser = auditLog.UserId > 0 ? await _context.Users.FindAsync(auditLog.UserId) : null;

            var baseEmailParams = new EmailTemplateParameters
            {
                TravelRequest = travelRequest,
                Requester = requester, // This is the Employee User object
                ProjectDetails = projectDetails,
                ActionBaseUrl = $"{_appSettings.BaseUrl}/api/emailactions"
            };

            // --- Workflow Logic based on NewStatusId ---
            switch (auditLog.NewStatusId)
            {
                case StatusPendingReview:
                    await HandleRequestSubmitted(baseEmailParams, managerActualEmail, managerAsUserForTemplate, duHeadActualEmail, duHeadAsUserForTemplate, adminUsers);
                    break;
                case StatusVerifiedByManager:
                    await HandleManagerApproved(baseEmailParams, duHeadActualEmail, duHeadAsUserForTemplate, adminUsers);
                    break;
                case StatusDuApprovedByDuHead:
                    await HandleDuHeadApproved(baseEmailParams, adminUsers, managerActualEmail, managerAsUserForTemplate);
                    break;
                case StatusOptionsListedByAdmin:
                    if (!string.IsNullOrWhiteSpace(managerActualEmail))
                        await HandleTicketOptionsSentToManager(baseEmailParams, managerActualEmail, managerAsUserForTemplate);
                    else
                        _logger.LogWarning("Manager email missing in RMT to send ticket options for TR {TRID}.", travelRequest.RequestId);
                    break;
                case StatusOptionSelectedByManager:
                    await HandleTicketBooked(baseEmailParams, managerActualEmail, managerAsUserForTemplate, duHeadActualEmail, duHeadAsUserForTemplate, adminUsers);
                    break;
                case StatusRejected:
                    string rejectedByRole = "Unknown";
                    string excludedEmail = null; // Email of the person who rejected
                    if (auditLog.ActionType == "ManagerRejected") { rejectedByRole = "Manager"; excludedEmail = managerActualEmail; }
                    else if (auditLog.ActionType == "DuHeadRejected") { rejectedByRole = "DU Head"; excludedEmail = duHeadActualEmail; }
                    else { _logger.LogWarning("Rejected status with unhandled ActionType: {Action}", auditLog.ActionType); }
                    await HandleRequestRejected(baseEmailParams, rejectedByRole, auditLog.Comments, managerActualEmail, duHeadActualEmail, adminUsers, excludedEmail);
                    break;
                case StatusCancelled:
                    await HandleRequestCancelled(baseEmailParams, managerActualEmail, duHeadActualEmail, adminUsers, actorUser?.EmployeeName ?? "System");
                    break;
                default:
                    _logger.LogWarning("Unhandled NewStatusId {NewStatus} for TR {TRID}.", auditLog.NewStatusId, travelRequest.RequestId);
                    break;
            }
        }

        // Helper to create EmailTemplateParameters, now including ActualRecipientEmail
        private EmailTemplateParameters CreateEmailParams(
            EmailTemplateParameters baseP,
            User recipientUserForSalutation = null,
            string actualEmailForSendingAndLinks = null, // Critical for sending and link generation
            List<TicketOptionInfoNoToken> ticketOpts = null,
            string selOptDesc = null)
        {
            return new EmailTemplateParameters
            {
                TravelRequest = baseP.TravelRequest,
                Requester = baseP.Requester,
                ProjectDetails = baseP.ProjectDetails,
                ActionBaseUrl = baseP.ActionBaseUrl,
                RecipientForSalutation = recipientUserForSalutation, // For "Hi {Name}" if User object exists
                ActualRecipientEmail = actualEmailForSendingAndLinks, // Email to send TO and use in links
                TicketOptions = ticketOpts,
                SelectedOptionDescription = selOptDesc
            };
        }

        // --- Handler Methods ---
        // These methods now use the 'actualEmail' for sending and 'userForTemplate' for the DTO's Recipient field.

        private async Task HandleRequestSubmitted(EmailTemplateParameters p, string managerActualEmail, User managerUserForTemplate, string duHeadActualEmail, User duHeadUserForTemplate, List<User> admins)
        {
            // 1. To Employee (Requester)
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetRequestSubmittedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            // 2. To Manager (For Approval)
            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: managerUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                    actualEmailForSendingAndLinks: managerActualEmail);
                var (manSub, manBody) = await _emailTemplateService.GetManagerApprovalRequestEmailAsync(manEmailParams);
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }
            else { _logger.LogWarning("Manager email missing in RMT for TR {TRID}", p.TravelRequest.RequestId); }

            // 3. To DU Head (Notification)
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: duHeadUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" },
                    actualEmailForSendingAndLinks: duHeadActualEmail);
                var (duSub, duBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(duEmailParams,
                    $"New travel request {p.TravelRequest.RequestId} by {p.Requester.EmployeeName} is pending manager approval.");
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }

            // 4. To Admins (Notification)
            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p); // No specific recipient user/actual email needed for "Admin Team"
                var (admSub, admBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(admEmailParams,
                    $"New travel request {p.TravelRequest.RequestId} by {p.Requester.EmployeeName} is pending manager approval.");
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }

        private async Task HandleManagerApproved(EmailTemplateParameters p, string duHeadActualEmail, User duHeadUserForTemplate, List<User> admins)
        {
            // 1. To Employee (Requester)
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(empEmailParams,
                $"Your request {p.TravelRequest.RequestId} was approved by manager. Pending DU Head approval.");
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            // 2. To DU Head (For Approval)
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: duHeadUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" },
                    actualEmailForSendingAndLinks: duHeadActualEmail);
                var (duSub, duBody) = await _emailTemplateService.GetDuHeadApprovalRequestEmailAsync(duEmailParams);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }
            else { _logger.LogWarning("DU Head email missing in RMT for TR {TRID} after manager approval.", p.TravelRequest.RequestId); }

            // 3. To Admins
            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p);
                var (admSub, admBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(admEmailParams,
                    $"Request {p.TravelRequest.RequestId} approved by manager. Pending DU Head approval.");
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }
        private async Task HandleDuHeadApproved(EmailTemplateParameters p, List<User> admins, string managerActualEmail, User managerUserForTemplate)
        {
            // 1. To Employee
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetRequestApprovedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            // 2. To Manager (Notification)
            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: managerUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                    actualEmailForSendingAndLinks: managerActualEmail);
                var (manSub, manBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(manEmailParams,
                    $"Request {p.TravelRequest.RequestId} for {p.Requester.EmployeeName} was approved by DU Head. Admin will provide ticket options.");
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }

            // 3. To Admins (Action Required)
            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p);
                var (admSub, admBody) = await _emailTemplateService.GetInformAdminForTicketOptionsEmailAsync(admEmailParams);
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }

        private async Task HandleTicketOptionsSentToManager(EmailTemplateParameters p, string managerActualEmail, User managerUserForTemplate)
        {
            if (string.IsNullOrWhiteSpace(managerActualEmail)) { /* Already checked before call */ return; }

            var ticketOptionsFromDb = await _context.TicketOptions
                .Where(to => to.RequestId == p.TravelRequest.RequestId && !to.IsSelected)
                .OrderBy(to => to.CreatedAt).ToListAsync();

            if (!ticketOptionsFromDb.Any()) { _logger.LogWarning("No ticket options for TR {TRID}", p.TravelRequest.RequestId); return; }

            var optionInfos = ticketOptionsFromDb.Select(opt => new TicketOptionInfoNoToken { OptionId = opt.OptionId, Description = opt.OptionDescription }).ToList();
            var manEmailParams = CreateEmailParams(p,
                recipientUserForSalutation: managerUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                actualEmailForSendingAndLinks: managerActualEmail,
                ticketOpts: optionInfos);
            var (manSub, manBody) = await _emailTemplateService.GetTicketOptionsForManagerEmailAsync(manEmailParams);
            await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);

            // Notify Employee
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail);
            var (empSub, empBody) = await _emailTemplateService.GetGeneralNotificationEmailAsync(empEmailParams,
                $"Ticket options for your request {p.TravelRequest.RequestId} sent to manager for selection.");
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);
        }

        private async Task HandleTicketBooked(EmailTemplateParameters p, string managerActualEmail, User managerUserForTemplate, string duHeadActualEmail, User duHeadUserForTemplate, List<User> admins)
        {
            var selOptDesc = p.TravelRequest.SelectedTicketOption?.OptionDescription ?? "N/A - Check Portal";

            // 1. To Employee
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail, selOptDesc: selOptDesc);
            var (empSub, empBody) = await _emailTemplateService.GetTicketBookedEmailAsync(empEmailParams);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empSub, empBody);

            // 2. To Manager
            if (!string.IsNullOrWhiteSpace(managerActualEmail))
            {
                var manEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: managerUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                    actualEmailForSendingAndLinks: managerActualEmail,
                    selOptDesc: selOptDesc);
                var (manSub, manBody) = await _emailTemplateService.GetTicketBookedEmailAsync(manEmailParams);
                await _notificationService.SendEmailAsync(managerActualEmail, manSub, manBody);
            }
            // 3. To DU Head
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail))
            {
                var duEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: duHeadUserForTemplate ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" },
                    actualEmailForSendingAndLinks: duHeadActualEmail,
                    selOptDesc: selOptDesc);
                var (duSub, duBody) = await _emailTemplateService.GetTicketBookedEmailAsync(duEmailParams);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duSub, duBody);
            }
            // 4. To Admins
            var adminEmails = admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p, selOptDesc: selOptDesc);
                var (admSub, admBody) = await _emailTemplateService.GetTicketBookedEmailAsync(admEmailParams);
                await _notificationService.SendEmailAsync(adminEmails, admSub, admBody);
            }
        }

        private async Task HandleRequestRejected(EmailTemplateParameters p, string rejectedByRole, string comments, string managerActualEmail, string duHeadActualEmail, List<User> admins, string excludedEmail)
        {
            // Notify Requester
            var empEmailParams = CreateEmailParams(p, recipientUserForSalutation: p.Requester, actualEmailForSendingAndLinks: p.Requester.EmployeeEmail);
            var (empRejSub, empRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(empEmailParams, rejectedByRole, comments);
            await _notificationService.SendEmailAsync(p.Requester.EmployeeEmail, empRejSub, empRejBody);

            // Notify Manager (if not the one who rejected)
            if (!string.IsNullOrWhiteSpace(managerActualEmail) && !managerActualEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
            {
                User managerUserObj = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == managerActualEmail && u.IsActive);
                var manEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: managerUserObj ?? new User { EmployeeName = p.ProjectDetails.ProjectManager ?? "Manager" },
                    actualEmailForSendingAndLinks: managerActualEmail);
                var (manRejSub, manRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(manEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(managerActualEmail, manRejSub, manRejBody);
            }

            // Notify DU Head (if not the one who rejected)
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail) && !duHeadActualEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
            {
                User duHeadUserObj = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail == duHeadActualEmail && u.IsActive);
                var duEmailParams = CreateEmailParams(p,
                    recipientUserForSalutation: duHeadUserObj ?? new User { EmployeeName = p.ProjectDetails.DuHeadName ?? "DU Head" },
                    actualEmailForSendingAndLinks: duHeadActualEmail);
                var (duRejSub, duRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(duEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(duHeadActualEmail, duRejSub, duRejBody);
            }

            // Notify Admins
            var adminEmails = admins
                .Where(a => !string.IsNullOrWhiteSpace(a.EmployeeEmail) && !a.EmployeeEmail.Equals(excludedEmail, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.EmployeeEmail)
                .ToList();
            if (adminEmails.Any())
            {
                var admEmailParams = CreateEmailParams(p);
                var (admRejSub, admRejBody) = await _emailTemplateService.GetRequestRejectedEmailAsync(admEmailParams, rejectedByRole, comments);
                await _notificationService.SendEmailAsync(adminEmails, admRejSub, admRejBody);
            }
        }

        private async Task HandleRequestCancelled(EmailTemplateParameters p, string managerActualEmail, string duHeadActualEmail, List<User> admins, string cancelledBy)
        {
            var message = $"Travel request {p.TravelRequest.RequestId} has been cancelled by {cancelledBy}.";
            var recipientEmails = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.Requester?.EmployeeEmail)) recipientEmails.Add(p.Requester.EmployeeEmail);
            if (!string.IsNullOrWhiteSpace(managerActualEmail)) recipientEmails.Add(managerActualEmail);
            if (!string.IsNullOrWhiteSpace(duHeadActualEmail)) recipientEmails.Add(duHeadActualEmail);
            recipientEmails.AddRange(admins.Select(a => a.EmployeeEmail).Where(e => !string.IsNullOrWhiteSpace(e)));

            var distinctEmails = recipientEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (distinctEmails.Any())
            {
                var cancelEmailParams = CreateEmailParams(p);
                var (sub, body) = await _emailTemplateService.GetGeneralNotificationEmailAsync(cancelEmailParams, message);
                await _notificationService.SendEmailAsync(distinctEmails, sub, body);
            }
        }
    }
}