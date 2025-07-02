using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.BackgroundServices;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;


namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalsController : ControllerBase
    {
        private readonly ITravelRequestServices _travelRequestService;
        private readonly IAuditLogServices _auditLogService;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ApiDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ApprovalsController> _logger; // <<< ADDED for logging
        protected APIResponse _response;

        public ApprovalsController(
            ITravelRequestServices travelRequestService,
            IAuditLogServices auditLogService,
            IBackgroundTaskQueue taskQueue,
         
            ApiDbContext context,
            IMapper mapper,
            ILogger<ApprovalsController> logger) // <<< ADDED ILogger injection
        {
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _context = context;
            _taskQueue = taskQueue;
            _mapper = mapper;
            _response = new APIResponse();
            _logger = logger; // <<< ADDED assign logger
        }

        private const int PENDING_REVIEW_STATUS_ID = 1;
        private const int VERIFIED_STATUS_ID = 2;
        private const int OPTIONS_SELECTED_STATUS_ID = 4; // Note: Your DUHeadApprove uses this
        private const int DU_APPROVED_STATUS_ID = 5;
        private const int REJECTED_STATUS_ID = 12;


        // Manager Approval
        [HttpPut("{requestId}/manager/approve")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> ManagerApprove(
            string requestId,
            [FromBody] ManagerApprovalDTO approvalDto)
        {
            _response = new APIResponse();
            _logger.LogInformation("PUT /manager/approve called for RequestId: {RequestId}, ApprovingUserId: {UserId}", requestId, approvalDto.ApprovingUserId);


            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(_response);
            }

            // Check if manager is correct
            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null)
            {
                _response.IsSuccess = false; _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Configuration error: Project details for {travelRequest.ProjectCode} not found.");
                _logger.LogError("ManagerApprove: RMT not found for ProjectCode {PC}", travelRequest.ProjectCode);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            var managerUser = await _context.Users.FindAsync(approvalDto.ApprovingUserId);
            if (managerUser == null || !managerUser.IsActive ||
                projectDetails.ProjectManagerEmail?.Equals(managerUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
            {
                _response.IsSuccess = false; _response.StatusCode = HttpStatusCode.Forbidden;
                _response.ErrorMessages.Add($"User {approvalDto.ApprovingUserId} is not authorized or not found for this manager approval.");
                _logger.LogWarning("ManagerApprove: Unauthorized user {UserId} or email mismatch for PM {PMEmail} on TR {ReqId}", approvalDto.ApprovingUserId, projectDetails.ProjectManagerEmail, requestId);
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }


            if (travelRequest.CurrentStatusId != PENDING_REVIEW_STATUS_ID)
            {
                var currentStatus = await _context.RequestStatuses.FindAsync(travelRequest.CurrentStatusId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Travel request is not pending manager approval. Current status: '{currentStatus?.StatusName ?? travelRequest.CurrentStatusId.ToString()}'");
                return Conflict(_response);
            }

            var managerApprovedStatus = await _context.RequestStatuses.FindAsync(VERIFIED_STATUS_ID);
            if (managerApprovedStatus == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Configuration error: 'Manager Approved' status (ID: {VERIFIED_STATUS_ID}) not found.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.CurrentStatusId = VERIFIED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
                _logger.LogInformation("ManagerApprove: TravelRequest {ReqId} status updated to {NewStatusId}", requestId, VERIFIED_STATUS_ID);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error approving request: {ex.Message}");
                _logger.LogError(ex, "ManagerApprove: Error during _travelRequestService.UpdateAsync for TR {ReqId}", requestId);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusEntity = await _context.RequestStatuses.FindAsync(oldStatusId);
            var oldStatusName = oldStatusEntity?.StatusName ?? oldStatusId.ToString();
            var newStatusName = managerApprovedStatus.StatusName;

            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = approvalDto.ApprovingUserId,
                ActionType = "ManagerApproved",
                OldStatusId = oldStatusId,
                NewStatusId = VERIFIED_STATUS_ID,
                //Comments = approvalDto.Comments,
                ActionDate = DateTime.UtcNow,       
                Timestamp = DateTime.UtcNow,        
                ChangeDescription = $"Manager ({managerUser.EmployeeName}) approved. Status changed from '{oldStatusName}' to '{newStatusName}'."
            };

            try
            {
                await _auditLogService.AddAsync(auditLogEntry);
                _logger.LogInformation("ManagerApprove: AuditLog {LogId} for TR {ReqId} saved.", auditLogEntry.LogId, requestId);

                // --- CALL AUDIT LOG HANDLER SERVICE TO TRIGGER EMxAILS ---
                _logger.LogInformation("ManagerApprove: AuditLogHandlerService processed for AuditLog {LogId}, TR {ReqId}.", auditLogEntry.LogId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManagerApprove: Error during AuditLog saving or Email processing for TR {ReqId} after approval. Approval stands.", requestId);
               
            }

            await _taskQueue.QueueBackgroundWorkItemAsync(auditLogEntry.LogId);
            var updatedRequestDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = new
            {
                UpdatedRequest = updatedRequestDto,
                ApprovalAudit = auditLogDto
            };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }


        // Manager Rejection
        [HttpPut("{requestId}/manager/reject")]
        public async Task<ActionResult<APIResponse>> ManagerReject(string requestId, [FromBody] ManagerRejectionDTO rejectionDto)
        {
            _response = new APIResponse();
            _logger.LogInformation("PUT /manager/reject for TR {ReqId}, RejectingUserId: {UserId}", requestId, rejectionDto.RejectingUserId);

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null) { /* NotFound */ return NotFound(/* ... */); }

            var actorUser = await _context.Users.FindAsync(rejectionDto.RejectingUserId);
            if (actorUser == null || !actorUser.IsActive) { /* UnprocessableEntity */ return UnprocessableEntity(/* ... */); }

            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null) { /* UnprocessableEntity */ return UnprocessableEntity(/* ... */); }

            if (projectDetails.ProjectManagerEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new APIResponse { /* ... */ });
            }


            if (travelRequest.CurrentStatusId != PENDING_REVIEW_STATUS_ID)
            {
                return Conflict(new APIResponse { /* ... */ });
            }

            var rejectedStatusEntity = await _context.RequestStatuses.FindAsync(REJECTED_STATUS_ID);
            if (rejectedStatusEntity == null) { /* InternalServerError */ }

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.CurrentStatusId = REJECTED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
                var auditLogEntry = new AuditLog
                {
                    RequestId = travelRequest.RequestId,
                    UserId = rejectionDto.RejectingUserId,
                    ActionType = "ManagerRejected",
                    OldStatusId = oldStatusId,
                    NewStatusId = REJECTED_STATUS_ID,
                    //Comments = rejectionDto.Comments,
                    ActionDate = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow,
                    ChangeDescription = $"Manager ({actorUser.EmployeeName}) rejected. Status changed from '{_context.RequestStatuses.Find(oldStatusId)?.StatusName ?? oldStatusId.ToString()}' to '{rejectedStatusEntity.StatusName}'."
                };
                await _auditLogService.AddAsync(auditLogEntry);
                _logger.LogInformation("ManagerReject: AuditLog {LogId} saved for TR {ReqId}", auditLogEntry.LogId, requestId);

                await _taskQueue.QueueBackgroundWorkItemAsync(auditLogEntry.LogId);
                _logger.LogInformation("ManagerReject: AuditLogHandlerService processed for AuditLog {LogId}, TR {ReqId}", auditLogEntry.LogId, requestId);

                _response.IsSuccess = true;
                _response.Result = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManagerReject: Error for TR {ReqId}", requestId);
                _response.IsSuccess = false; _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error rejecting request: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        // DU Head Approval
        [HttpPut("{requestId}/duhead/approve")]
        public async Task<ActionResult<APIResponse>> DUHeadApprove(string requestId, [FromBody] DUHeadApprovalDTO approvalDto)
        {
            _response = new APIResponse();
            _logger.LogInformation("PUT /duhead/approve for TR {ReqId}, ApprovingUserId: {UserId}", requestId, approvalDto.ApprovingUserId);


            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null) { /* NotFound */ }

            var actorUser = await _context.Users.FindAsync(approvalDto.ApprovingUserId);
            if (actorUser == null || !actorUser.IsActive) { /* UnprocessableEntity */ }

            var projectDetails = await _context.RMTs.FirstOrDefaultAsync(r => r.ProjectCode == travelRequest.ProjectCode);
            if (projectDetails == null) { /* UnprocessableEntity */ }

            if (projectDetails.DuHeadEmail?.Equals(actorUser.EmployeeEmail, StringComparison.OrdinalIgnoreCase) != true)
            {
            }

            if (travelRequest.CurrentStatusId != VERIFIED_STATUS_ID) 
            {
                /* Conflict */
                var currentStatus = await _context.RequestStatuses.FindAsync(travelRequest.CurrentStatusId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Travel request is not pending DU Head approval. Current status: '{currentStatus?.StatusName ?? travelRequest.CurrentStatusId.ToString()}'");
                return Conflict(_response);
            }

            var duApprovedStatusEntity = await _context.RequestStatuses.FindAsync(DU_APPROVED_STATUS_ID);
            if (duApprovedStatusEntity == null) { /* InternalServerError */ }

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.CurrentStatusId = DU_APPROVED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;
            AuditLog auditLogEntry = null;
            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
                auditLogEntry = new AuditLog
                {
                    RequestId = travelRequest.RequestId,
                    UserId = approvalDto.ApprovingUserId,
                    ActionType = "DuHeadApproved",
                    OldStatusId = oldStatusId,
                    NewStatusId = DU_APPROVED_STATUS_ID,
                    //Comments = approvalDto.Comments,
                    ActionDate = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow,
                    ChangeDescription = $"DU Head ({actorUser.EmployeeName}) approved. Status changed from '{_context.RequestStatuses.Find(oldStatusId)?.StatusName ?? oldStatusId.ToString()}' to '{duApprovedStatusEntity.StatusName}'."
                };
                await _auditLogService.AddAsync(auditLogEntry);
                _logger.LogInformation("DUHeadApprove: AuditLog {LogId} saved for TR {ReqId}", auditLogEntry.LogId, requestId);

                // --- CALL AUDIT LOG HANDLER SERVICE TO TRIGGER EMAILS ---
                //await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLogEntry);
                await _taskQueue.QueueBackgroundWorkItemAsync(auditLogEntry.LogId);
                _logger.LogInformation("DUHeadApprove: AuditLogHandlerService processed for AuditLog {LogId}, TR {ReqId}", auditLogEntry.LogId, requestId);

                _response.IsSuccess = true;
                _response.Result = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DUHeadApprove: Error for TR {ReqId}. AuditLogData: {@AuditLog}", requestId, auditLogEntry);
                _response.IsSuccess = false; _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error approving by DU Head: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
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
                /* ... styles ... */
                </style></head><body><div class='container {(title.Contains("Successful") || title.Contains("Approved") ? "success" : title.Contains("Denied") || title.Contains("Error") || title.Contains("Rejected") ? "error" : "info")}'>
                <h1>{title}</h1><p>{message}</p>
                {(string.IsNullOrWhiteSpace(additionalInfo) ? "" : $"<p><small>{additionalInfo}</small></p>")}
                <p style='margin-top: 25px;'><a href='javascript:if(window.opener){{window.close();}}else{{alert(""You can close this tab."");}}'>Close this window</a></p>
                </div></body></html>";
            return Content(html, "text/html");
        }
    }
}