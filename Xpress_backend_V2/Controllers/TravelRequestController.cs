using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Repository;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelRequestsController : ControllerBase
    {
        private readonly ITravelRequestServices _travelRequestService;
        private readonly IAuditLogServices _auditLogService;
        private readonly IMapper _mapper;
        private readonly ApiDbContext _context;
        private readonly ILogger<TravelRequestsController> _logger;

        private const int DefaultInitialStatusId = 1;

        public TravelRequestsController(
            ITravelRequestServices travelRequestService,
            ApiDbContext context,
            IAuditLogServices auditLogService,
            IMapper mapper,
            ILogger<TravelRequestsController> logger)
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

        // GetTravelRequestById method is NOT included here as per your request
        // to only have the POST method.

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

        // Travel Request Details APIs

        // Travel InfoBanner APIs
        [HttpGet("infobanner/{requestId}")]
        public async Task<IActionResult> GetTravelInfoBannerDetails(string requestId)
        {
            var details = await _travelRequestService.GetTravelInfoBannerDetailsAsync(requestId);
            if (details == null || !details.Any())
            {
                return NotFound($"No travel request found with RequestId = {requestId}");
            }
            return Ok(details);
        }
    }
}

    