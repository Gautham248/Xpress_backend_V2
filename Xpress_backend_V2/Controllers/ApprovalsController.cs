using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ApiDbContext _context;
        private readonly IMapper _mapper;
        protected APIResponse _response;

        public ApprovalsController(
            ITravelRequestServices travelRequestService,
            IAuditLogServices auditLogService,
            ApiDbContext context,
            IMapper mapper)
        {
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _context = context;
            _mapper = mapper;
            _response = new APIResponse();
        }

        private const int PENDING_REVIEW_STATUS_ID = 1;
        private const int VERIFIED_STATUS_ID = 2;
        private const int OPTIONS_SELECTED_STATUS_ID = 4;
        private const int DU_APPROVED_STATUS_ID = 5;
        private const int REJECTED_STATUS_ID = 12;


        // Manager Approval
        [HttpPut("{requestId}/manager/approve")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> ManagerApprove(
            string requestId, // Assuming TravelRequest.RequestId is string
            [FromBody] ManagerApprovalDTO approvalDto)
        {
            _response = new APIResponse(); // Initialize for each call

            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            // 1. Fetch the travel request
            // Assuming GetByRequestIdAsync is the method in your service that takes the string RequestId
            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(_response);
            }

            // 2. Validate current status
            // Ensure the request is actually pending manager approval
            if (travelRequest.CurrentStatusId != PENDING_REVIEW_STATUS_ID)
            {
                var currentStatus = await _context.RequestStatuses.FindAsync(travelRequest.CurrentStatusId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict; // 409 Conflict is appropriate here
                _response.ErrorMessages.Add($"Travel request is not pending manager approval. Current status: '{currentStatus?.StatusName ?? travelRequest.CurrentStatusId.ToString()}'");
                return Conflict(_response);
            }

            // 3. Validate the "Manager Approved" status exists (good practice, though it should)
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
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Concurrency error updating request. It might have been modified by someone else. {ex.Message}");
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error approving request: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusEntity = await _context.RequestStatuses.FindAsync(oldStatusId);
            var oldStatusName = oldStatusEntity?.StatusName ?? oldStatusId.ToString();
            var newStatusName = managerApprovedStatus.StatusName;

            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = approvalDto.ApprovingUserId,
                ActionType = "VERIFIED",
                OldStatusId = oldStatusId,
                NewStatusId = VERIFIED_STATUS_ID,
                Comments = approvalDto.Comments,
                ChangeDescription = $"Manager approved. Status changed from '{oldStatusName}' to '{newStatusName}'."
            };

            await _auditLogService.AddAsync(auditLogEntry);

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

        // You can add other approval-related endpoints here:
        // - ManagerReject (PUT .../manager/reject)
        // - FinanceApprove (PUT .../finance/approve)
        // - FinanceReject (PUT .../finance/reject)
        // - GetPendingApprovalsForUser (GET /pending/{userId})


        // Manager Rejection
        [HttpPut("{requestId}/manager/reject")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> ManagerReject(
    string requestId,
    [FromBody] ManagerRejectionDTO rejectionDto)
        {
            _response = new APIResponse();

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

            // Validate current status - expecting "PendingReview"
            if (travelRequest.CurrentStatusId != PENDING_REVIEW_STATUS_ID)
            {
                var currentStatus = await _context.RequestStatuses.FindAsync(travelRequest.CurrentStatusId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Travel request is not pending review/manager action. Current status: '{currentStatus?.StatusName ?? travelRequest.CurrentStatusId.ToString()}'");
                return Conflict(_response);
            }

            var rejectedStatusEntity = await _context.RequestStatuses.FindAsync(REJECTED_STATUS_ID);
            if (rejectedStatusEntity == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Configuration error: 'Rejected' status (ID: {REJECTED_STATUS_ID}) not found.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.CurrentStatusId = REJECTED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Concurrency error: {ex.Message}");
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error rejecting request: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusEntity = await _context.RequestStatuses.FindAsync(oldStatusId);
            var oldStatusName = oldStatusEntity?.StatusName ?? oldStatusId.ToString();
            var newStatusName = rejectedStatusEntity.StatusName;

            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = rejectionDto.RejectingUserId,
                ActionType = "MANAGER_REJECTED",
                OldStatusId = oldStatusId,
                NewStatusId = REJECTED_STATUS_ID,
                Comments = rejectionDto.Comments,
                ChangeDescription = $"Manager rejected. Status changed from '{oldStatusName}' to '{newStatusName}'."
            };
            await _auditLogService.AddAsync(auditLogEntry);

            var updatedRequestDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = new { UpdatedRequest = updatedRequestDto, RejectionAudit = auditLogDto };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }


        // DU Head Approval
        [HttpPut("{requestId}/duhead/approve")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> DUHeadApprove(
    string requestId,
    [FromBody] DUHeadApprovalDTO approvalDto)
        {
            _response = new APIResponse();

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

            // Validate current status - expecting "OptionsListed"
            if (travelRequest.CurrentStatusId != OPTIONS_SELECTED_STATUS_ID)
            {
                var currentStatus = await _context.RequestStatuses.FindAsync(travelRequest.CurrentStatusId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Travel request is not in 'Options Selected' state for DU Head approval. Current status: '{currentStatus?.StatusName ?? travelRequest.CurrentStatusId.ToString()}'");
                return Conflict(_response);
            }

            var duApprovedStatusEntity = await _context.RequestStatuses.FindAsync(DU_APPROVED_STATUS_ID);
            if (duApprovedStatusEntity == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Configuration error: 'DUApproved' status (ID: {DU_APPROVED_STATUS_ID}) not found.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.CurrentStatusId = DU_APPROVED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Concurrency error: {ex.Message}");
                return Conflict(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error approving request by DU Head: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var oldStatusEntity = await _context.RequestStatuses.FindAsync(oldStatusId);
            var oldStatusName = oldStatusEntity?.StatusName ?? oldStatusId.ToString();
            var newStatusName = duApprovedStatusEntity.StatusName;

            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = approvalDto.ApprovingUserId,
                ActionType = "DU_HEAD_APPROVED",
                OldStatusId = oldStatusId,
                NewStatusId = DU_APPROVED_STATUS_ID,
                Comments = approvalDto.Comments,
                ChangeDescription = $"DU Head approved. Status changed from '{oldStatusName}' to '{newStatusName}'."
            };
            await _auditLogService.AddAsync(auditLogEntry);

            var updatedRequestDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = new { UpdatedRequest = updatedRequestDto, ApprovalAudit = auditLogDto };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}
