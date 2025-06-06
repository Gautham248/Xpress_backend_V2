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
    public class TravelRequestController : ControllerBase
    {
        private readonly ITravelRequestServices _travelRequestService;
        private readonly ApiDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditLogServices _auditLogService;

        private readonly ILogger<TravelRequestController> _logger;
        private readonly IAuditLogHandlerService _auditLogHandlerService;
        protected APIResponse _response;


        private const int DefaultInitialStatusId = 1;


        public TravelRequestController(ITravelRequestServices travelRequestService,
            ApiDbContext context,
            IAuditLogServices auditLogService,
            IAuditLogHandlerService auditLogHandler,
            IMapper mapper,
            ILogger<TravelRequestController> logger)
        {
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _context = context;
            _logger = logger;
            _auditLogHandlerService = auditLogHandler;
            _response = new APIResponse();
        }
        private DateTime EnsureUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return dt.ToUniversalTime();
        }
        private DateTime? EnsureUtc(DateTime? dt)
        {
            return dt.HasValue ? EnsureUtc(dt.Value) : null;
        }

        private string GenerateStatusChangeComment(int newStatusId, string actionType, StatusTransition transition)
        {
            return newStatusId switch
            {
                2 => "Request verified by Manager",
                5 => "Request approved by DU Head",
                6 => "Request approved by BU Manager",
                12 => "Request rejected by approver",
                _ => actionType switch
                {
                    "APPROVED" => "Request approved",
                    "REJECTED" => "Request rejected",
                    _ => $"Status changed to {transition.NewStatusName}"
                }
            };
        }

        private string GetApproverRole(int statusId)
        {
            return statusId switch
            {
                5 => "DU Head",
                6 => "BU Manager",
                _ => "Approver"
            };
        }

        private string GenerateStatusChangeDescription(string oldStatusName, string newStatusName, string actionType, StatusTransition transition)
        {
            return actionType switch
            {
                "APPROVED" => $"Changed from {oldStatusName} to {newStatusName}",
                "REJECTED" => $"Changed from {oldStatusName} to {newStatusName}",
                _ => $"Status changed from {oldStatusName} to {newStatusName}"
            };
        }

        private StatusTransition GetStatusTransitionDetails(int oldStatusId, int newStatusId)
        {
            var oldStatus = _context.RequestStatuses.Find(oldStatusId);
            var newStatus = _context.RequestStatuses.Find(newStatusId);

            return new StatusTransition
            {
                OldStatusName = oldStatus?.StatusName ?? oldStatusId.ToString(),
                NewStatusName = newStatus?.StatusName ?? newStatusId.ToString(),
                IsApproval = newStatusId == 5 || newStatusId == 6,
                IsRejection = newStatusId == 12
            };
        }

        private record StatusTransition
        {
            public string OldStatusName { get; init; }
            public string NewStatusName { get; init; }
            public bool IsApproval { get; init; }
            public bool IsRejection { get; init; }
        }

        [HttpPost]
        [ProducesResponseType(typeof(TravelRequestResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTravelRequest([FromBody] TravelRequestCreateDTO travelRequestCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var travelRequestEntity = _mapper.Map<TravelRequest>(travelRequestCreateDto);

                travelRequestEntity.OutboundDepartureDate = EnsureUtc(travelRequestCreateDto.OutboundDepartureDate);
                travelRequestEntity.OutboundArrivalDate = EnsureUtc(travelRequestCreateDto.OutboundArrivalDate);
                travelRequestEntity.ReturnDepartureDate = EnsureUtc(travelRequestCreateDto.ReturnDepartureDate);
                travelRequestEntity.ReturnArrivalDate = EnsureUtc(travelRequestCreateDto.ReturnArrivalDate);

                travelRequestEntity.RequestId = Guid.NewGuid().ToString("N");
                travelRequestEntity.CurrentStatusId = DefaultInitialStatusId;
                travelRequestEntity.IsActive = true;

                if (travelRequestEntity.OutboundArrivalDate <= travelRequestEntity.OutboundDepartureDate)
                {
                    ModelState.AddModelError(nameof(travelRequestCreateDto.OutboundArrivalDate), "Outbound arrival date must be after outbound departure date.");
                }

                if (travelRequestEntity.IsRoundTrip)
                {
                    if (!travelRequestEntity.ReturnDepartureDate.HasValue || !travelRequestEntity.ReturnArrivalDate.HasValue)
                    {
                        ModelState.AddModelError(nameof(travelRequestCreateDto.IsRoundTrip), "Return departure and arrival dates are required for round trips.");
                    }
                    else
                    {
                        if (travelRequestEntity.ReturnDepartureDate.Value <= travelRequestEntity.OutboundArrivalDate)
                        {
                            ModelState.AddModelError(nameof(travelRequestCreateDto.ReturnDepartureDate), "Return departure date must be after outbound arrival date.");
                        }
                        if (travelRequestEntity.ReturnArrivalDate.Value <= travelRequestEntity.ReturnDepartureDate.Value)
                        {
                            ModelState.AddModelError(nameof(travelRequestCreateDto.ReturnArrivalDate), "Return arrival date must be after return departure date.");
                        }
                    }
                }
                else
                {
                    travelRequestEntity.ReturnDepartureDate = null;
                    travelRequestEntity.ReturnArrivalDate = null;
                }

                // Additional validation for pickup/drop-off fields
                if (travelRequestEntity.IsPickUpRequired && string.IsNullOrWhiteSpace(travelRequestEntity.PickUpPlace))
                {
                    ModelState.AddModelError(nameof(travelRequestCreateDto.PickUpPlace), "Pick-up place is required when pick-up is requested.");
                }

                if (travelRequestEntity.IsDropOffRequired && string.IsNullOrWhiteSpace(travelRequestEntity.DropOffPlace))
                {
                    ModelState.AddModelError(nameof(travelRequestCreateDto.DropOffPlace), "Drop-off place is required when drop-off is requested.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                TravelRequest createdTravelRequest = await _travelRequestService.CreateTravelRequestAsync(travelRequestEntity);

                var auditLog = new AuditLog
                {
                    RequestId = createdTravelRequest.RequestId,
                    UserId = createdTravelRequest.UserId,
                    ActionType = "REQUEST_CREATED",
                    OldStatusId = null,
                    NewStatusId = createdTravelRequest.CurrentStatusId,
                    ChangeDescription = "New travel request created.",
                    ActionDate = DateTime.UtcNow,
                    Timestamp = createdTravelRequest.CreatedAt
                };
                await _auditLogService.CreateAuditLogAsync(auditLog);
                await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLog);

                var responseDto = _mapper.Map<TravelRequestResponseDTO>(createdTravelRequest);

                // MODIFIED PART: Return 201 Created with the object in the body, but no Location header
                return StatusCode(StatusCodes.Status201Created, responseDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error occurred while creating travel request for UserID {UserId}. Check foreign key constraints.", travelRequestCreateDto.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Ensure all referenced IDs (UserId, TravelModeId, ProjectCode) are valid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating travel request for UserID {UserId}.", travelRequestCreateDto.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        //UPDATE TRAVEL REQEUEST ( +AUDIT LOG ENTRY)
        [HttpPut("update/{requestId}")]
        [ProducesResponseType(typeof(TravelRequestResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTravelRequest(string requestId, [FromBody] TravelRequestCreateDTO travelRequestUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if the travel request exists
                var existingTravelRequest = await _travelRequestService.GetTravelRequestByIdAsync(requestId);
                if (existingTravelRequest == null)
                {
                    return NotFound($"Travel request with ID {requestId} not found.");
                }

                // Map the DTO to entity
                var travelRequestEntity = _mapper.Map<TravelRequest>(travelRequestUpdateDto);
                travelRequestEntity.RequestId = requestId; // Ensure the ID remains the same
                travelRequestEntity.CurrentStatusId = 1; // Set status to 1 as requested
                travelRequestEntity.IsActive = existingTravelRequest.IsActive; // Preserve existing IsActive status

                // Ensure UTC for date fields
                travelRequestEntity.OutboundDepartureDate = EnsureUtc(travelRequestUpdateDto.OutboundDepartureDate);
                travelRequestEntity.OutboundArrivalDate = EnsureUtc(travelRequestUpdateDto.OutboundArrivalDate);
                travelRequestEntity.ReturnDepartureDate = EnsureUtc(travelRequestUpdateDto.ReturnDepartureDate);
                travelRequestEntity.ReturnArrivalDate = EnsureUtc(travelRequestUpdateDto.ReturnArrivalDate);

                // Validation for date fields
                if (travelRequestEntity.OutboundArrivalDate <= travelRequestEntity.OutboundDepartureDate)
                {
                    ModelState.AddModelError(nameof(travelRequestUpdateDto.OutboundArrivalDate), "Outbound arrival date must be after outbound departure date.");
                }

                if (travelRequestEntity.IsRoundTrip)
                {
                    if (!travelRequestEntity.ReturnDepartureDate.HasValue || !travelRequestEntity.ReturnArrivalDate.HasValue)
                    {
                        ModelState.AddModelError(nameof(travelRequestUpdateDto.IsRoundTrip), "Return departure and arrival dates are required for round trips.");
                    }
                    else
                    {
                        if (travelRequestEntity.ReturnDepartureDate.Value <= travelRequestEntity.OutboundArrivalDate)
                        {
                            ModelState.AddModelError(nameof(travelRequestUpdateDto.ReturnDepartureDate), "Return departure date must be after outbound arrival date.");
                        }
                        if (travelRequestEntity.ReturnArrivalDate.Value <= travelRequestEntity.ReturnDepartureDate.Value)
                        {
                            ModelState.AddModelError(nameof(travelRequestUpdateDto.ReturnArrivalDate), "Return arrival date must be after return departure date.");
                        }
                    }
                }
                else
                {
                    travelRequestEntity.ReturnDepartureDate = null;
                    travelRequestEntity.ReturnArrivalDate = null;
                }

                // Additional validation for pickup/drop-off fields
                if (travelRequestEntity.IsPickUpRequired && string.IsNullOrWhiteSpace(travelRequestEntity.PickUpPlace))
                {
                    ModelState.AddModelError(nameof(travelRequestUpdateDto.PickUpPlace), "Pick-up place is required when pick-up is requested.");
                }

                if (travelRequestEntity.IsDropOffRequired && string.IsNullOrWhiteSpace(travelRequestEntity.DropOffPlace))
                {
                    ModelState.AddModelError(nameof(travelRequestUpdateDto.DropOffPlace), "Drop-off place is required when drop-off is requested.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Update the travel request
                TravelRequest updatedTravelRequest = await _travelRequestService.UpdateTravelRequestAsync(travelRequestEntity);

                // Create audit log entry
                var auditLog = new AuditLog
                {
                    RequestId = updatedTravelRequest.RequestId,
                    UserId = updatedTravelRequest.UserId,
                    ActionType = "REQUEST_MODIFIED",
                    OldStatusId = existingTravelRequest.CurrentStatusId,
                    NewStatusId = updatedTravelRequest.CurrentStatusId,
                    ChangeDescription = "Travel request modified.",
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogService.CreateAuditLogAsync(auditLog);

                // Map to response DTO
                var responseDto = _mapper.Map<TravelRequestResponseDTO>(updatedTravelRequest);

                return Ok(responseDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error occurred while updating travel request ID {RequestId}. Check foreign key constraints.", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred. Ensure all referenced IDs (UserId, TravelModeId, ProjectCode) are valid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating travel request ID {RequestId}.", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpGet("{requestId}")]
        [ProducesResponseType(typeof(TravelRequestResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTravelRequestById(string requestId)
        {
            try
            {
                _logger.LogInformation("Fetching travel request with ID {RequestId}.", requestId);

                var travelRequest = await _travelRequestService.GetTravelRequestByIdAsync(requestId);
                if (travelRequest == null)
                {
                    _logger.LogWarning("Travel request with ID {RequestId} not found.", requestId);
                    return NotFound($"Travel request with ID {requestId} not found.");
                }

                var responseDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);

                _logger.LogInformation("Successfully fetched travel request with ID {RequestId}.", requestId);
                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching travel request with ID {RequestId}.", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
            }
        }


        [HttpGet("travelrequests")]
        public async Task<ActionResult<IEnumerable<TravelRequestDTO>>> GetTravelRequests()
        {
            var travelRequests = await _context.TravelRequests
                .Include(t => t.User)
                .Include(t => t.Project)
                .Include(t => t.TravelMode)
                .Include(t => t.CurrentStatus)
                .Include(t => t.SelectedTicketOption)
                .ToListAsync();

            var travelRequestDtos = _mapper.Map<List<TravelRequestDTO>>(travelRequests);
            return Ok(travelRequestDtos);
        }


        // GET: api/TravelRequest/ByProjectManager/{email}
        [HttpGet("ByProjectManager/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetActiveTravelRequestsByProjectManager(string email)
        {
            var apiResponse = new APIResponse();

            if (string.IsNullOrWhiteSpace(email))
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.BadRequest;
                apiResponse.ErrorMessages.Add("Project manager email is required.");
                return BadRequest(apiResponse);
            }

            var travelRequests = await _context.TravelRequests
                .Where(tr => tr.Project.ProjectManagerEmail == email && tr.IsActive)
                .Include(tr => tr.Project)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.User)
                .Select(tr => new TravelRequestDTO
                {
                    RequestId = tr.RequestId,
                    SourcePlace = tr.SourcePlace,
                    SourceCountry = tr.SourceCountry,
                    DestinationPlace = tr.DestinationPlace,
                    DestinationCountry = tr.DestinationCountry,
                    OutboundDepartureDate = tr.OutboundDepartureDate,
                    OutboundArrivalDate = tr.OutboundArrivalDate,
                    ReturnDepartureDate = tr.ReturnDepartureDate,
                    ReturnArrivalDate = tr.ReturnArrivalDate,
                    IsAccommodationRequired = tr.IsAccommodationRequired,
                    IsPickupRequired = tr.IsPickUpRequired,
                    IsDropoffRequired = tr.IsDropOffRequired,
                    PickupPlace = tr.IsPickUpRequired ? tr.PickUpPlace : null,
                    DropoffPlace = tr.IsDropOffRequired ? tr.DropOffPlace : null,
                    Comments = tr.Comments,
                    PurposeOfTravel = tr.PurposeOfTravel,
                    IsVegetarian = tr.IsVegetarian,
                    AttendedCct = tr.AttendedCCT,
                    TravelAgencyName = tr.TravelAgencyName,
                    TotalExpense = tr.TotalExpense,
                    UploadedTicketPdfPath = tr.TicketDocumentPath,
                    CreatedAt = tr.CreatedAt,
                    UpdatedAt = tr.UpdatedAt,
                    EmployeeName = tr.User.EmployeeName,
                    IsInternational = tr.IsInternational,
                    IsRoundTrip = tr.IsRoundTrip,
                    ProjectName = tr.Project.ProjectName,
                    TravelModeName = tr.TravelMode.TravelModeName,
                    CurrentStatusName = tr.CurrentStatus.StatusName,
                    SelectedTicketOptionId = tr.SelectedTicketOptionId
                })
                .ToListAsync();

            if (!travelRequests.Any())
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.NotFound;
                apiResponse.ErrorMessages.Add("No active travel requests found for the specified project manager.");
                return NotFound(apiResponse);
            }

            apiResponse.IsSuccess = true;
            apiResponse.StatusCode = HttpStatusCode.OK;
            apiResponse.Result = travelRequests;
            return Ok(apiResponse);
        }

        //Travel Request By DU Head
        [HttpGet("ByDUH/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetActiveTravelRequestsByDUHead(string email)
        {
            var apiResponse = new APIResponse();

            if (string.IsNullOrWhiteSpace(email))
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.BadRequest;
                apiResponse.ErrorMessages.Add("DU Head email is required.");
                return BadRequest(apiResponse);
            }

            var travelRequests = await _context.TravelRequests
                .Where(tr => tr.Project.DuHeadEmail == email && tr.IsActive)
                .Include(tr => tr.Project)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.User)
                .Select(tr => new TravelRequestDTO
                {
                    RequestId = tr.RequestId,
                    SourcePlace = tr.SourcePlace,
                    SourceCountry = tr.SourceCountry,
                    DestinationPlace = tr.DestinationPlace,
                    DestinationCountry = tr.DestinationCountry,
                    OutboundDepartureDate = tr.OutboundDepartureDate,
                    OutboundArrivalDate = tr.OutboundArrivalDate,
                    ReturnDepartureDate = tr.ReturnDepartureDate,
                    ReturnArrivalDate = tr.ReturnArrivalDate,
                    IsAccommodationRequired = tr.IsAccommodationRequired,
                    IsPickupRequired = tr.IsPickUpRequired,
                    IsDropoffRequired = tr.IsDropOffRequired,
                    PickupPlace = tr.IsPickUpRequired ? tr.PickUpPlace : null,
                    DropoffPlace = tr.IsDropOffRequired ? tr.DropOffPlace : null,
                    Comments = tr.Comments,
                    PurposeOfTravel = tr.PurposeOfTravel,
                    IsVegetarian = tr.IsVegetarian,
                    AttendedCct = tr.AttendedCCT,
                    TravelAgencyName = tr.TravelAgencyName,
                    TotalExpense = tr.TotalExpense,
                    UploadedTicketPdfPath = tr.TicketDocumentPath,
                    CreatedAt = tr.CreatedAt,
                    UpdatedAt = tr.UpdatedAt,
                    EmployeeName = tr.User.EmployeeName,
                    IsInternational = tr.IsInternational,
                    IsRoundTrip = tr.IsRoundTrip,
                    ProjectName = tr.Project.ProjectName,
                    TravelModeName = tr.TravelMode.TravelModeName,
                    CurrentStatusName = tr.CurrentStatus.StatusName,
                    SelectedTicketOptionId = tr.SelectedTicketOptionId
                })
                .ToListAsync();

            if (!travelRequests.Any())
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.NotFound;
                apiResponse.ErrorMessages.Add("No active travel requests found for the specified project manager.");
                return NotFound(apiResponse);
            }

            apiResponse.IsSuccess = true;
            apiResponse.StatusCode = HttpStatusCode.OK;
            apiResponse.Result = travelRequests;
            return Ok(apiResponse);
        }

        
        // Travel InfoBanner API
        [HttpGet("infobanner/{requestId}")]
        public async Task<ActionResult<APIResponse>> GetTravelInfoBannerDetails(string requestId)
        {
            var response = new APIResponse();

            try
            {
                var details = await _travelRequestService.GetTravelInfoBannerDetailsAsync(requestId);

                if (details == null || !details.Any())
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add($"No travel request found with RequestId = {requestId}");
                    response.Result = null;

                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = details;

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    response.ErrorMessages.Add("An error occurred while retrieving travel information");
                    response.ErrorMessages.Add(ex.Message);
                    response.Result = null;

                return StatusCode(500, response);
            }
        }

        // Travel Info API
        [HttpGet("travelinfo/{requestId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetTravelInfoDetails(string requestId)
        {
            var localResponse = new APIResponse();

            if (string.IsNullOrWhiteSpace(requestId))
            {
                localResponse.IsSuccess = false;
                localResponse.StatusCode = HttpStatusCode.BadRequest;
                localResponse.ErrorMessages.Add("Request ID cannot be empty.");
                return BadRequest(localResponse);
            }

            var travelInfo = await _travelRequestService.GetTravelInfoAsync(requestId);

            if (travelInfo == null || !travelInfo.Any())
            {
                localResponse.IsSuccess = false;
                localResponse.StatusCode = HttpStatusCode.NotFound;
                localResponse.ErrorMessages.Add($"No travel information found for RequestId = {requestId}");
                return NotFound(localResponse);
            }

            localResponse.IsSuccess = true;
            localResponse.Result = travelInfo;
            localResponse.StatusCode = HttpStatusCode.OK;
            return Ok(localResponse);
        }


        // Update Status
        [HttpPut("{requestId}/updatestatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> UpdateTravelRequestStatus(
    [FromBody] UpdateTravelRequestStatusDTO statusUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            // Validate the new status exists
            var newStatus = await _context.RequestStatuses.FindAsync(statusUpdateDto.NewStatusId);
            if (newStatus == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"Invalid status ID: {statusUpdateDto.NewStatusId}");
                return BadRequest(_response);
            }

            // Get the travel request
            var travelRequest = await _travelRequestService.GetByIdAsync(statusUpdateDto.RequestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel request not found: {statusUpdateDto.RequestId}");
                return NotFound(_response);
            }

            // Store old status for audit log
            var oldStatusId = travelRequest.CurrentStatusId;

            // Update the status
            travelRequest.CurrentStatusId = statusUpdateDto.NewStatusId;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error updating request: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            // Create audit log entry
            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = statusUpdateDto.UserId,
                ActionType = "STATUS_UPDATED",
                OldStatusId = oldStatusId,
                NewStatusId = statusUpdateDto.NewStatusId,
                Comments = statusUpdateDto.Comments,
                ChangeDescription = $"Status changed from {oldStatusId} to {statusUpdateDto.NewStatusId}"
            };

            // Get status names if available
            var oldStatusName = (await _context.RequestStatuses.FindAsync(oldStatusId))?.StatusName ?? oldStatusId.ToString();
            var newStatusName = newStatus.StatusName;
            auditLogEntry.ChangeDescription = $"Status changed from '{oldStatusName}' to '{newStatusName}'";

            await _auditLogService.AddAsync(auditLogEntry);

            // Return updated travel request and audit log
            var updatedRequestDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = new
            {
                UpdatedRequest = updatedRequestDto,
                AuditLog = auditLogDto
            };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}