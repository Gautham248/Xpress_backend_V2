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

        private const int DefaultInitialStatusId = 1;


        public TravelRequestController(ITravelRequestServices travelRequestService,
            ApiDbContext context,
            IAuditLogServices auditLogService,
            IMapper mapper,
            ILogger<TravelRequestController> logger)
        {
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _mapper = mapper;
            _context = context;
            _logger = logger;
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
                    Timestamp = createdTravelRequest.CreatedAt
                };
                await _auditLogService.CreateAuditLogAsync(auditLog);

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

        // Travel Request APIs
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
        public async Task<ActionResult<IEnumerable<TravelRequestDTO>>> GetActiveTravelRequestsByProjectManager(string email)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Project manager email is required.");
            }

            // Query to get active travel requests for the project manager's project
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
                    PickupLocation = tr.IsPickUpRequired ? tr.SourcePlace : null, // Assuming pickup location is same as SourcePlace
                    DropoffLocation = tr.IsDropOffRequired ? tr.DestinationPlace : null, // Assuming dropoff location is same as DestinationPlace
                    Comments = tr.Comments,
                    PurposeOfTravel = tr.PurposeOfTravel,
                    IsVegetarian = tr.IsVegetarian,
                    AttendedCct = tr.AttendedCCT,
                    TravelAgencyName = tr.TravelAgencyName,
                    TotalExpense = tr.TotalExpense,
                    UploadedTicketPdfPath = tr.TicketDocumentPath,
                    CreatedAt = tr.CreatedAt,
                    UpdatedAt = tr.UpdatedAt,
                    EmployeeName = tr.User.EmployeeName, // Assuming User has a Name property
                    IsInternational = tr.IsInternational,
                    IsRoundTrip = tr.IsRoundTrip,
                    ProjectName = tr.Project.ProjectName,
                    TravelModeName = tr.TravelMode.TravelModeName, // Assuming TravelMode has a Name property
                    CurrentStatusName = tr.CurrentStatus.StatusName, // Assuming RequestStatus has a Name property
                    SelectedTicketOptionId = tr.SelectedTicketOptionId
                })
                .ToListAsync();

            if (!travelRequests.Any())
            {
                return NotFound("No active travel requests found for the specified project manager.");
            }

            return Ok(travelRequests);
        }

        // Travel Request Details APIs

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
    }
}