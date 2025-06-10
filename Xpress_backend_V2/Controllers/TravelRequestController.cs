using System.Net;
using System.Security.Claims;
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
        private readonly IHttpClientFactory _httpClientFactory;


        private const int DefaultInitialStatusId = 1;
        private const int TICKET_UPLOADED_STATUS_ID = 7;

        public TravelRequestController(ITravelRequestServices travelRequestService,
            ApiDbContext context,
            IAuditLogServices auditLogService,
            IAuditLogHandlerService auditLogHandler,
            IMapper mapper,
            ILogger<TravelRequestController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _context = context;
            _logger = logger;
            _auditLogHandlerService = auditLogHandler;
            _response = new APIResponse();
            _httpClientFactory = httpClientFactory;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
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

        [HttpGet("ByUser/{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetActiveTravelRequestsByUserId(int userId)
        {
            var apiResponse = new APIResponse();
            if (userId <= 0)
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.BadRequest;
                apiResponse.ErrorMessages.Add("User ID must be a positive integer.");
                return BadRequest(apiResponse);
            }
            try
            {
                var travelRequests = await _travelRequestService.GetTravelRequestsByUserIdAsync(userId);
                if (!travelRequests.Any())
                {
                    apiResponse.IsSuccess = false;
                    apiResponse.StatusCode = HttpStatusCode.NotFound;
                    apiResponse.ErrorMessages.Add("No active travel requests found for the specified user.");
                    return NotFound(apiResponse);
                }
                // Sort travel requests by CreatedAt in descending order (latest first - chronological order)
                var sortedTravelRequests = travelRequests.OrderByDescending(tr => tr.CreatedAt).ToList();
                apiResponse.IsSuccess = true;
                apiResponse.StatusCode = HttpStatusCode.OK;
                apiResponse.Result = sortedTravelRequests;
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving travel requests for UserId {UserId}.", userId);
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                apiResponse.ErrorMessages.Add("An error occurred while retrieving travel requests.");
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        [HttpGet("{requestId}/timeline")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetTimeline(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Travel Request ID cannot be empty.");
                return BadRequest(_response);
            }

            var timeline = await _travelRequestService.GetTimelineAsync(requestId);
            if (timeline == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"No timeline data found for request ID {requestId}.");
                return NotFound(_response);
            }

            _response.IsSuccess = true;
            _response.Result = timeline;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
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
                travelRequestEntity.OutboundArrivalDate = travelRequestCreateDto.OutboundArrivalDate.HasValue
                    ? EnsureUtc(travelRequestCreateDto.OutboundArrivalDate.Value) : null;

                travelRequestEntity.ReturnDepartureDate = travelRequestCreateDto.ReturnDepartureDate.HasValue
                    ? EnsureUtc(travelRequestCreateDto.ReturnDepartureDate.Value) : null;

                travelRequestEntity.ReturnArrivalDate = travelRequestCreateDto.ReturnArrivalDate.HasValue
                    ? EnsureUtc(travelRequestCreateDto.ReturnArrivalDate.Value) : null;

                travelRequestEntity.RequestId = GenerateCustomRequestId(travelRequestCreateDto);
                travelRequestEntity.CurrentStatusId = DefaultInitialStatusId;
                travelRequestEntity.IsActive = true;
                travelRequestEntity.CreatedAt = DateTime.UtcNow;



                if (travelRequestEntity.OutboundArrivalDate.HasValue &&
                    travelRequestEntity.OutboundArrivalDate.Value <= travelRequestEntity.OutboundDepartureDate)
                {
                    ModelState.AddModelError(nameof(travelRequestCreateDto.OutboundArrivalDate), "Outbound arrival date must be after outbound departure date.");
                }

                if (travelRequestEntity.IsRoundTrip)
                {
                    if (!travelRequestEntity.ReturnDepartureDate.HasValue)
                    {
                        ModelState.AddModelError(nameof(travelRequestCreateDto.ReturnDepartureDate), "Return departure date is required for a round trip.");
                    }
                    else 
                    {
                        
                        if (travelRequestEntity.OutboundArrivalDate.HasValue &&
                            travelRequestEntity.ReturnDepartureDate.Value <= travelRequestEntity.OutboundArrivalDate.Value)
                        {
                            ModelState.AddModelError(nameof(travelRequestCreateDto.ReturnDepartureDate), "Return departure date must be after outbound arrival date.");
                        }

                        
                        if (travelRequestEntity.ReturnArrivalDate.HasValue)
                        {
                            if (travelRequestEntity.ReturnArrivalDate.Value <= travelRequestEntity.ReturnDepartureDate.Value)
                            {
                                ModelState.AddModelError(nameof(travelRequestCreateDto.ReturnArrivalDate), "Return arrival date must be after return departure date.");
                            }
                        }
                    }
                }
                else
                {
                    travelRequestEntity.ReturnDepartureDate = null;
                    travelRequestEntity.ReturnArrivalDate = null;
                }

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

        private string GenerateCustomRequestId(TravelRequestCreateDTO dto)
        {
            string travelType = dto.IsInternational ? "1" : "0";

            string transportMode = GetTransportModeCode(dto.TravelModeId);

            string tripType = dto.IsRoundTrip ? "1" : "0";

            string sequence = GenerateRandomSequence(6);

            return $"{travelType}{transportMode}{tripType}{sequence}";
        }

        private string GetTransportModeCode(int travelModeId)
        {

            return travelModeId switch
            {
                1 => "F", 
                2 => "T", 
                3 => "B", 
                4 => "C", 
                _ => "F"  
            };
        }

       
        private string GenerateRandomSequence(int length)
        {
            var random = new Random();
            var sequence = "";

            for (int i = 0; i < length; i++)
            {
                sequence += random.Next(0, 10).ToString();
            }

            return sequence;
        }





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
                var existingTravelRequest = await _travelRequestService.GetTravelRequestByIdAsync(requestId);
                if (existingTravelRequest == null)
                {
                    return NotFound($"Travel request with ID {requestId} not found.");
                }

                var travelRequestEntity = _mapper.Map<TravelRequest>(travelRequestUpdateDto);
                travelRequestEntity.RequestId = requestId;
                travelRequestEntity.CurrentStatusId = 1;
                travelRequestEntity.IsActive = existingTravelRequest.IsActive;

                travelRequestEntity.OutboundDepartureDate = EnsureUtc(travelRequestUpdateDto.OutboundDepartureDate);
                travelRequestEntity.OutboundArrivalDate = EnsureUtc(travelRequestUpdateDto.OutboundArrivalDate);
                travelRequestEntity.ReturnDepartureDate = EnsureUtc(travelRequestUpdateDto.ReturnDepartureDate);
                travelRequestEntity.ReturnArrivalDate = EnsureUtc(travelRequestUpdateDto.ReturnArrivalDate);

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

                TravelRequest updatedTravelRequest = await _travelRequestService.UpdateTravelRequestAsync(travelRequestEntity);

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


        [HttpGet("travelrequests")]
        public async Task<ActionResult<IEnumerable<TravelRequestDTO>>> GetTravelRequests()
        {
            var travelRequests = await _context.TravelRequests
                .Include(t => t.User)
                .Include(t => t.Project)
                .Include(t => t.TravelMode)
                .Include(t => t.CurrentStatus)
                .Include(t => t.SelectedTicketOption)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.CreatedAt) // Sort by creation time, latest first
                .Select(t => new TravelRequestDTO
                {
                    RequestId = t.RequestId,
                    SourcePlace = t.SourcePlace,
                    SourceCountry = t.SourceCountry,
                    DestinationPlace = t.DestinationPlace,
                    DestinationCountry = t.DestinationCountry,
                    OutboundDepartureDate = t.OutboundDepartureDate,
                    OutboundArrivalDate = t.OutboundArrivalDate,
                    ReturnDepartureDate = t.ReturnDepartureDate,
                    ReturnArrivalDate = t.ReturnArrivalDate,
                    IsAccommodationRequired = t.IsAccommodationRequired,
                    IsPickupRequired = t.IsPickUpRequired,
                    IsDropoffRequired = t.IsDropOffRequired,
                    PickupPlace = t.IsPickUpRequired ? t.PickUpPlace : null,
                    DropoffPlace = t.IsDropOffRequired ? t.DropOffPlace : null,
                    Comments = t.Comments,
                    PurposeOfTravel = t.PurposeOfTravel,
                    IsVegetarian = t.IsVegetarian,
                    AttendedCct = t.AttendedCCT,
                    TravelAgencyName = t.TravelAgencyName,
                    TotalExpense = t.TotalExpense,
                    UploadedTicketPdfPath = t.TicketDocumentPath,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    EmployeeName = t.User != null ? t.User.EmployeeName : "Unknown",
                    IsInternational = t.IsInternational,
                    IsRoundTrip = t.IsRoundTrip,
                    ProjectName = t.Project != null ? t.Project.ProjectName : "Unknown",
                    TravelModeName = t.TravelMode != null ? t.TravelMode.TravelModeName : "Unknown",
                    CurrentStatusName = t.CurrentStatus != null ? t.CurrentStatus.StatusName : "Unknown",
                    SelectedTicketOptionId = t.SelectedTicketOption != null ? t.SelectedTicketOptionId : null,
                    DuId = t.Project != null ? t.Project.DuId : 0,
                    ProjectManagerName = t.Project != null ? t.Project.ProjectManager : "Unknown"
                })
                .ToListAsync();

            if (!travelRequests.Any())
            {
                return NotFound(new { message = "No active travel requests found." });
            }

            return Ok(travelRequests);
        }

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
                .OrderByDescending(tr => tr.CreatedAt) // Added sorting by CreatedAt in descending order
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
                    SelectedTicketOptionId = tr.SelectedTicketOptionId,
                    // Add DU ID and Project Manager Name from the Project entity
                    DuId = tr.Project.DuId,
                    ProjectManagerName = tr.Project.ProjectManager
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
                .OrderByDescending(tr => tr.CreatedAt) // Added sorting by CreatedAt in descending order
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
                    SelectedTicketOptionId = tr.SelectedTicketOptionId,
                    // Add DU ID and Project Manager Name from the Project entity to match the first endpoint
                    DuId = tr.Project.DuId,
                    ProjectManagerName = tr.Project.ProjectManager
                })
                .ToListAsync();
            if (!travelRequests.Any())
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.NotFound;
                apiResponse.ErrorMessages.Add("No active travel requests found for the specified DU Head.");
                return NotFound(apiResponse);
            }
            apiResponse.IsSuccess = true;
            apiResponse.StatusCode = HttpStatusCode.OK;
            apiResponse.Result = travelRequests;
            return Ok(apiResponse);
        }


        [HttpGet("{requestId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetTravelRequestById(string requestId)
        {
            var localResponse = new APIResponse();

            if (string.IsNullOrWhiteSpace(requestId))
            {
                localResponse.IsSuccess = false;
                localResponse.StatusCode = HttpStatusCode.BadRequest;
                localResponse.ErrorMessages.Add("Travel Request ID cannot be empty.");
                return BadRequest(localResponse);
            }

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);

            if (travelRequest == null)
            {
                localResponse.IsSuccess = false;
                localResponse.StatusCode = HttpStatusCode.NotFound;
                localResponse.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(localResponse);
            }

            var responseDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);

            localResponse.IsSuccess = true;
            localResponse.StatusCode = HttpStatusCode.OK;
            localResponse.Result = responseDto;
            return Ok(localResponse);
        }

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

            var newStatus = await _context.RequestStatuses.FindAsync(statusUpdateDto.NewStatusId);
            if (newStatus == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"Invalid status ID: {statusUpdateDto.NewStatusId}");
                return BadRequest(_response);
            }

            var travelRequest = await _travelRequestService.GetByIdAsync(statusUpdateDto.RequestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel request not found: {statusUpdateDto.RequestId}");
                return NotFound(_response);
            }

            var oldStatusId = travelRequest.CurrentStatusId;
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

            var oldStatusName = (await _context.RequestStatuses.FindAsync(oldStatusId))?.StatusName ?? oldStatusId.ToString();
            var newStatusName = newStatus.StatusName;
            auditLogEntry.ChangeDescription = $"Status changed from '{oldStatusName}' to '{newStatusName}'";

            await _auditLogService.AddAsync(auditLogEntry);

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

        // Feedback Submission
        [HttpPut("{requestId}/travelfeedback")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> SubmitTravelFeedback(
        string requestId,
        [FromBody] SubmitTravelFeedbackDTO feedbackDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            var authenticatedUserId = GetCurrentUserId();

            var userIdForAudit = feedbackDto.SubmittingUserId;

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(_response);
            }

            if (!string.IsNullOrEmpty(travelRequest.TravelFeedback))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Conflict;
                _response.ErrorMessages.Add($"Feedback has already been submitted for this travel request.");
                return Conflict(_response);
            }

            travelRequest.TravelFeedback = feedbackDto.FeedbackText;
            travelRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _travelRequestService.UpdateAsync(travelRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating travel request with feedback for ID {RequestId}", requestId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Error submitting feedback: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

            var auditLogEntry = new AuditLog
            {
                RequestId = travelRequest.RequestId,
                UserId = userIdForAudit,
                ActionType = "TRAVEL_FEEDBACK_SUBMITTED",
                ChangeDescription = $"Travel feedback submitted by user ID {userIdForAudit}.",
                Comments = $"Feedback: \"{feedbackDto.FeedbackText.Substring(0, Math.Min(feedbackDto.FeedbackText.Length, 200))}\""
            };

            try
            {
                await _auditLogService.AddAsync(auditLogEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding audit log for feedback submission on Request ID {RequestId}", requestId);
            }

            var updatedRequestDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = new
            {
                Message = "Travel feedback submitted successfully.",
                UpdatedRequest = updatedRequestDto,
                AuditLog = auditLogDto
            };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        // Upload Tickets Modal
        [HttpPut("{requestId}/uploadticketdetails")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadTicketDetails(string requestId, [FromBody] TravelRequestUploadTicketDTO uploadTicketDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            try
            {
                var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
                if (travelRequest == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages.Add($"Travel request with ID {requestId} not found.");
                    return NotFound(_response);
                }

                var oldStatusIdForAudit = travelRequest.CurrentStatusId;

                if (uploadTicketDto.Airlines != null && uploadTicketDto.Airlines.Any())
                {
                    foreach (var airlineDto in uploadTicketDto.Airlines)
                    {
                        var newAirlineSegment = new Airline
                        {
                            AirlineId = Math.Abs(Guid.NewGuid().GetHashCode()),
                            AirlineName = airlineDto.Name,
                            AirlineExpense = (double)airlineDto.Cost,
                            RequestId = requestId
                        };

                        _context.Airlines.Add(newAirlineSegment);
                        _logger.LogInformation("Prepared airline segment: {AirlineName} for Request ID: {RequestId}", newAirlineSegment.AirlineName, requestId);
                        
                    }
                }

                travelRequest.TravelAgencyName = uploadTicketDto.TravelAgencyName;
                travelRequest.TravelAgencyExpense = uploadTicketDto.AgencyBookingCharge;
                travelRequest.TotalExpense = uploadTicketDto.TotalExpense;
                travelRequest.TicketDocumentPath = uploadTicketDto.PdfFilePath;

                travelRequest.UpdatedAt = DateTime.UtcNow;
                travelRequest.CurrentStatusId = TICKET_UPLOADED_STATUS_ID;

                _context.Entry(travelRequest).State = EntityState.Modified;
                await _context.SaveChangesAsync();


                var auditLog = new AuditLog
                {
                    RequestId = travelRequest.RequestId,
                    UserId = travelRequest.UserId,
                    ActionType = "TICKET_DETAILS_UPLOADED",
                    OldStatusId = oldStatusIdForAudit,
                    NewStatusId = travelRequest.CurrentStatusId,
                    ChangeDescription = "Ticket details and airline information uploaded.",
                    Comments = $"Agency: {travelRequest.TravelAgencyName}, Total: {travelRequest.TotalExpense}",
                    Timestamp = DateTime.UtcNow
                };
                await _auditLogService.AddAsync(auditLog);

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                var responseDto = _mapper.Map<TravelRequestResponseDTO>(travelRequest);
                _response.Result = responseDto;
                return Ok(_response);

            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error occurred while uploading ticket details for Request ID {RequestId}.", requestId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("A database error occurred. Please ensure data is valid and try again.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while uploading ticket details for Request ID {RequestId}.", requestId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred. Please try again later.");
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        // To get distinct airlines
        [HttpGet("airlines")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetAirlineNames()
        {
            var response = new APIResponse();
            try
            {
                var airlineNames = await _context.Airlines
                                                 .Select(a => a.AirlineName)
                                                 .Distinct()
                                                 .OrderBy(name => name)
                                                 .ToListAsync();

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = airlineNames;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching airline names.");
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An unexpected error occurred while fetching airline names.");
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

        // To download the ticket file
        [HttpGet("{requestId}/downloadticket")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadTicket(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return BadRequest("Travel Request ID cannot be empty.");
            }

            try
            {
                // Find the URL for the ticket document
                var ticketPath = await _context.TravelRequests
                    .Where(tr => tr.RequestId == requestId)
                    .Select(tr => tr.TicketDocumentPath)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(ticketPath))
                {
                    return NotFound("No ticket document is available for this travel request.");
                }

                // Create an HttpClient to fetch the file from the URL
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(ticketPath);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch ticket from URL {Url}. Status: {StatusCode}", ticketPath, response.StatusCode);
                    return StatusCode((int)response.StatusCode, $"Could not retrieve the file from the source. Status: {response.StatusCode}");
                }

                var fileStream = await response.Content.ReadAsStreamAsync();

                var downloadFileName = $"Ticket-{requestId}.pdf";

                return File(fileStream, "application/pdf", downloadFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing ticket download for Request ID {RequestId}", requestId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An internal error occurred while processing your download request.");
            }
        }
    }
}