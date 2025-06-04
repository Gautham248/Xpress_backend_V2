using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
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