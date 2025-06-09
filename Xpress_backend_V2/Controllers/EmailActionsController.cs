using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.Configuration;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Data;
using Microsoft.EntityFrameworkCore;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailActionsController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IAuditLogHandlerService _auditLogHandlerService;
        private readonly ILogger<EmailActionsController> _logger;

        // Status ID constants - ensure these match your DB and AuditLogHandlerService
        private const int StatusPendingReview = 1;
        private const int StatusVerifiedByManager = 2;
        private const int StatusOptionsListedByAdmin = 3;
        private const int StatusOptionSelectedByManager = 4;
        private const int StatusDuApprovedByDuHead = 5;
        private const int StatusRejected = 12;

        public EmailActionsController(
            ApiDbContext context,
            IAuditLogHandlerService auditLogHandlerService,
            ILogger<EmailActionsController> logger)
        {
            _context = context;
            _auditLogHandlerService = auditLogHandlerService;
            _logger = logger;
        }

        // Generic processing logic for approve/reject actions
        private async Task<IActionResult> ProcessApprovalOrRejectionAction(
            string requestId,
            string actorEmail, // Email of the user supposed to be acting (from URL)
            Func<TravelRequest, User, RMT, Task<(int newStatusId, string auditActionType, string successMessage, string failureMessageIfWrongState)>> actionSpecificLogic,
            string actionNameForLog)
        {
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(actorEmail))
            {
                _logger.LogWarning("{ActionName}: RequestId or ActorEmail is missing from query.", actionNameForLog);
                return BadRequest(GenerateHtmlResponse("Error", "Invalid action link: Required information is missing."));
            }

            var travelRequest = await _context.TravelRequests
                                        .Include(tr => tr.CurrentStatus) // For OldStatusId and current status name
                                        .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

            if (travelRequest == null)
            {
                _logger.LogError("{ActionName}: TravelRequest {RequestId} not found.", actionNameForLog, requestId);
                return NotFound(GenerateHtmlResponse("Not Found", $"Travel Request '{requestId}' not found."));
            }

            var actorUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail.Equals(actorEmail, StringComparison.OrdinalIgnoreCase) && u.IsActive);
            if (actorUser == null)
            {
                _logger.LogError("{ActionName}: Actor user with email {ActorEmail} not found or inactive for RequestId {RequestId}.",
                    actionNameForLog, actorEmail, requestId);
                return UnprocessableEntity(GenerateHtmlResponse("Error", "Could not identify the acting user. Please ensure the email in the link is correct and the user account is active."));
            }

            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null)
            {
                _logger.LogError("{ActionName}: RMT project details not found for ProjectCode {ProjectCode} (RequestId: {RequestId}).", actionNameForLog, travelRequest.ProjectCode, requestId);
                return UnprocessableEntity(GenerateHtmlResponse("Configuration Error", "Project configuration details could not be found. Unable to verify approver."));
            }

            try
            {
                var (newStatusId, auditActionType, successMessage, failureMessageIfWrongState) = await actionSpecificLogic(travelRequest, actorUser, projectDetails);

                // The actionSpecificLogic should throw InvalidOperationException if state is wrong
                // or return a specific newStatusId (e.g. current status) if no change should occur.
                // If failureMessageIfWrongState is returned by actionSpecificLogic (and it's not null),
                // it implies the state was wrong but handled gracefully by the lambda.
                if (!string.IsNullOrWhiteSpace(failureMessageIfWrongState))
                {
                    return Ok(GenerateHtmlResponse("Action Not Applicable", failureMessageIfWrongState));
                }


                int oldStatusId = travelRequest.CurrentStatusId;
                travelRequest.CurrentStatusId = newStatusId;
                travelRequest.UpdatedAt = DateTime.UtcNow;

                _context.TravelRequests.Update(travelRequest);

                var auditLog = new AuditLog
                {
                    RequestId = travelRequest.RequestId,
                    UserId = actorUser.UserId,
                    ActionType = auditActionType,
                    OldStatusId = oldStatusId,
                    NewStatusId = newStatusId,
                    ChangeDescription = $"{actorUser.EmployeeName} ({actorEmail}) performed '{auditActionType}' via email link.",
                    ActionDate = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                    // Comments can be added if your DTO/flow supports it
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLog);

                _logger.LogInformation("{ActionName}: Successfully processed for RequestId {RequestId} by {ActorEmail}. New status: {NewStatusId}.",
                    actionNameForLog, requestId, actorEmail, newStatusId);
                return Ok(GenerateHtmlResponse("Action Successful", successMessage));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "{ActionName}: Unauthorized action attempt for RequestId {RequestId} by {ActorEmail}.", actionNameForLog, requestId, actorEmail);
                return StatusCode(StatusCodes.Status403Forbidden, GenerateHtmlResponse("Action Denied", ex.Message));
            }
            catch (InvalidOperationException ex) // For stale state or wrong conditions thrown by actionSpecificLogic
            {
                _logger.LogWarning(ex, "{ActionName}: Invalid operation for RequestId {RequestId}. Action by {ActorEmail}.", actionNameForLog, requestId, actorEmail);
                return Ok(GenerateHtmlResponse("Action Not Applicable", ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ActionName}: Error processing action for RequestId {RequestId} by {ActorEmail}.", actionNameForLog, requestId, actorEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, GenerateHtmlResponse("Error", "An unexpected error occurred. Please contact support."));
            }
        }

        [HttpGet("manager-approve")]
        public async Task<IActionResult> ManagerApproveRequest([FromQuery] string requestId, [FromQuery] string actorEmail)
        {
            return await ProcessApprovalOrRejectionAction(requestId, actorEmail,
            async (travelRequest, actorUser, projectDetails) =>
            {
                if (projectDetails.ProjectManagerEmail?.ToLower() != actorUser.EmployeeEmail.ToLower())
                {
                    throw new UnauthorizedAccessException($"User {actorUser.EmployeeName} is not the designated manager for this request.");
                }

                if (travelRequest.CurrentStatusId != StatusPendingReview)
                {
                    // Gracefully handle if already processed or in a different state
                    string message = $"Request {travelRequest.RequestId} is not pending manager review (current status: {travelRequest.CurrentStatus?.StatusName}). No action taken.";
                    _logger.LogInformation("ManagerApproveRequest: " + message);
                    return (travelRequest.CurrentStatusId, "NoChange-AlreadyProcessed", message, message);
                }

                await Task.CompletedTask;
                return (StatusVerifiedByManager, "ManagerApproved", $"Travel Request {travelRequest.RequestId} approved by you (Manager).", null);
            }, "ManagerApproveRequest");
        }


        [HttpGet("manager-reject")]
        public async Task<IActionResult> ManagerRejectRequest([FromQuery] string requestId, [FromQuery] string actorEmail)
        {
            return await ProcessApprovalOrRejectionAction(requestId, actorEmail,
           async (travelRequest, actorUser, projectDetails) =>
           {
               if (projectDetails.ProjectManagerEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
               {
                   throw new UnauthorizedAccessException($"User {actorUser.EmployeeName} is not the designated manager for this request.");
               }
               if (travelRequest.CurrentStatusId == StatusRejected)
               {
                   string message = $"Request {travelRequest.RequestId} is already rejected. No action taken.";
                   _logger.LogInformation("ManagerRejectRequest: " + message);
                   return (travelRequest.CurrentStatusId, "NoChange-AlreadyTerminal", message, message);
               }
               if (travelRequest.CurrentStatusId != StatusPendingReview && travelRequest.CurrentStatusId != StatusVerifiedByManager) // Manager can reject if pending their review or pending DU Head
               {
                   _logger.LogWarning("Manager rejecting {ReqId} not in ideal state (current: {CurStat}), proceeding.", travelRequest.RequestId, travelRequest.CurrentStatus?.StatusName);
               }
               await Task.CompletedTask;
               return (StatusRejected, "ManagerRejected", $"Travel Request {travelRequest.RequestId} rejected by you (Manager).", null);
           }, "ManagerRejectRequest");
        }

        [HttpGet("duhead-approve")]
        public async Task<IActionResult> DuHeadApproveRequest([FromQuery] string requestId, [FromQuery] string actorEmail)
        {
            return await ProcessApprovalOrRejectionAction(requestId, actorEmail,
            async (travelRequest, actorUser, projectDetails) =>
            {
                if (projectDetails.DuHeadEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
                {
                    throw new UnauthorizedAccessException($"User {actorUser.EmployeeName} is not the designated DU Head for this request.");
                }
                if (travelRequest.CurrentStatusId != StatusVerifiedByManager) // Must be approved by Manager first
                {
                    string message = $"Request {travelRequest.RequestId} is not pending DU Head review (current status: {travelRequest.CurrentStatus?.StatusName}). No action taken.";
                    _logger.LogInformation("DuHeadApproveRequest: " + message);
                    return (travelRequest.CurrentStatusId, "NoChange-AlreadyProcessed", message, message);
                }
                await Task.CompletedTask;
                return (StatusDuApprovedByDuHead, "DuHeadApproved", $"Travel Request {travelRequest.RequestId} approved by you (DU Head).", null);
            }, "DuHeadApproveRequest");
        }

        [HttpGet("duhead-reject")]
        public async Task<IActionResult> DuHeadRejectRequest([FromQuery] string requestId, [FromQuery] string actorEmail)
        {
            return await ProcessApprovalOrRejectionAction(requestId, actorEmail,
            async (travelRequest, actorUser, projectDetails) =>
            {
                if (projectDetails.DuHeadEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
                {
                    throw new UnauthorizedAccessException($"User {actorUser.EmployeeName} is not the designated DU Head for this request.");
                }
                if (travelRequest.CurrentStatusId == StatusRejected)
                {
                    string message = $"Request {travelRequest.RequestId} is already rejected. No action taken.";
                    _logger.LogInformation("DuHeadRejectRequest: " + message);
                    return (travelRequest.CurrentStatusId, "NoChange-AlreadyTerminal", message, message);
                }
                if (travelRequest.CurrentStatusId != StatusVerifiedByManager)
                {
                    _logger.LogWarning("DU Head rejecting {ReqId} not in ideal state (current: {CurStat}), proceeding.", travelRequest.RequestId, travelRequest.CurrentStatus?.StatusName);
                }
                await Task.CompletedTask;
                return (StatusRejected, "DuHeadRejected", $"Travel Request {travelRequest.RequestId} rejected by you (DU Head).", null);
            }, "DuHeadRejectRequest");
        }

        [HttpGet("select-ticket")]
        public async Task<IActionResult> SelectTicketOption(
            [FromQuery] string requestId,
            [FromQuery] string actorEmail, // Manager's email
            [FromQuery] int optionId)
        {
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(actorEmail) || optionId <= 0)
            {
                _logger.LogWarning("SelectTicketOption: RequestId, ActorEmail, or OptionId missing/invalid.");
                return BadRequest(GenerateHtmlResponse("Error", "Invalid action link: Missing information."));
            }

            var travelRequest = await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);
            if (travelRequest == null) return NotFound(GenerateHtmlResponse("Not Found", $"TR '{requestId}' not found."));

            var actorUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeEmail.Equals(actorEmail, StringComparison.OrdinalIgnoreCase) && u.IsActive);
            if (actorUser == null) return UnprocessableEntity(GenerateHtmlResponse("Error", "Acting user not identified."));

            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null) return UnprocessableEntity(GenerateHtmlResponse("Config Error", "Project details not found."));

            if (projectDetails.ProjectManagerEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, GenerateHtmlResponse("Action Denied", $"User {actorEmail} is not authorized for this action."));
            }

            if (travelRequest.CurrentStatusId != StatusOptionsListedByAdmin)
            {
                return Ok(GenerateHtmlResponse("Action Not Applicable", $"Request {requestId} not pending ticket selection (Status: {travelRequest.CurrentStatus?.StatusName})."));
            }

            var ticketOptionToSelect = await _context.TicketOptions.FirstOrDefaultAsync(to => to.OptionId == optionId && to.RequestId == requestId);
            if (ticketOptionToSelect == null) return NotFound(GenerateHtmlResponse("Not Found", $"Ticket option ID {optionId} not found for request {requestId}."));
            if (ticketOptionToSelect.IsSelected) return Ok(GenerateHtmlResponse("Action Not Applicable", $"Ticket option {optionId} already selected."));

            try
            {
                int oldStatusId = travelRequest.CurrentStatusId;
                travelRequest.CurrentStatusId = StatusOptionSelectedByManager;
                travelRequest.SelectedTicketOptionId = optionId;
                travelRequest.UpdatedAt = DateTime.UtcNow;
                ticketOptionToSelect.IsSelected = true;

                _context.TravelRequests.Update(travelRequest);
                _context.TicketOptions.Update(ticketOptionToSelect);

                var auditLog = new AuditLog
                {/* ... populate ... */
                    RequestId = travelRequest.RequestId,
                    UserId = actorUser.UserId,
                    ActionType = "TicketSelected",
                    OldStatusId = oldStatusId,
                    NewStatusId = StatusOptionSelectedByManager,
                    ChangeDescription = $"{actorUser.EmployeeName} selected ticket option ID {optionId}.",
                    ActionDate = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLog);

                return Ok(GenerateHtmlResponse("Action Successful", $"Ticket option for Request {requestId} selected."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelectTicketOption: Error for TR {ReqId}, Option {OptId}.", requestId, optionId);
                return StatusCode(StatusCodes.Status500InternalServerError, GenerateHtmlResponse("Error", "Unexpected error during ticket selection."));
            }
        }

        private ContentResult GenerateHtmlResponse(string title, string message, string additionalInfo = null)
        {
            string html = $@"
                <!DOCTYPE html><html><head><title>Travel Approval - {title}</title><style>
                body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f4f4f4; display: flex; justify-content: center; align-items: center; min-height: 90vh; text-align: center; }}
                .container {{ background-color: #fff; padding: 25px 40px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); max-width: 550px; }}
                h1 {{ color: #333; margin-top: 0; font-size: 1.8em; }} p {{ color: #555; font-size: 1.1em; line-height: 1.6; }}
                .success h1 {{ color: #28a745; }} .error h1 {{ color: #dc3545; }} .info h1 {{ color: #17a2b8; }}
                a {{ color: #007bff; text-decoration: none; font-weight: bold; }} a:hover {{ text-decoration: underline; }}
                small {{ color: #777; display: block; margin-top: 15px; }}
                </style></head><body><div class='container {(title.Contains("Successful") || title.Contains("Approved") ? "success" : title.Contains("Denied") || title.Contains("Error") || title.Contains("Rejected") ? "error" : "info")}'>
                <h1>{title}</h1><p>{message}</p>
                {(string.IsNullOrWhiteSpace(additionalInfo) ? "" : $"<p><small>{additionalInfo}</small></p>")}
                <p style='margin-top: 25px;'><a href='javascript:if(window.opener){{window.close();}}else{{alert(""You can close this tab."");}}'>Close this window</a></p>
                </div></body></html>";
            return Content(html, "text/html");
        }
    }
}

