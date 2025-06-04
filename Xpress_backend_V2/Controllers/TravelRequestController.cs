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

        public TravelRequestController(ITravelRequestServices travelRequestService, IMapper mapper, ApiDbContext context)
        {
            _travelRequestService = travelRequestService;
            _mapper = mapper;
            _context = context;
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

        // Travel InfoBanner APIs
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
                    response.ErrorMessages.Add(ex.Message); // Optional: Include actual error message for debugging
                    response.Result = null;

                    return StatusCode(500, response);
                }
            }
    }
}