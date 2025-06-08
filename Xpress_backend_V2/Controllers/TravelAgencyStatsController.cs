using Microsoft.AspNetCore.Mvc;
using System.Net; // Required for HttpStatusCode
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models; // Assuming APIResponse is in this namespace

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TravelAgencyStatsController : ControllerBase
    {
        private readonly ITravelAgencyStatRepository _statRepository;
        private readonly ILogger<TravelAgencyStatsController> _logger;
        protected APIResponse _response;

        // Inject ILogger for robust error handling
        public TravelAgencyStatsController(ITravelAgencyStatRepository statRepository, ILogger<TravelAgencyStatsController> logger)
        {
            _statRepository = statRepository;
            _logger = logger;
            _response = new APIResponse();
        }

        /// <summary>
        /// Gets aggregated statistics for travel agencies within a specified date range.
        /// </summary>
        /// <param name="startDate" example="2023-01-01">The start date for the report (YYYY-MM-DD).</param>
        /// <param name="endDate" example="2023-12-31">The end date for the report (YYYY-MM-DD).</param>
        /// <returns>A standardized API response containing a list of travel agency statistics.</returns>
        [HttpGet("stats")] // Using "stats" to be more explicit, e.g., GET api/TravelAgencyStats/stats
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetStats([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            // Handle validation error
            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Start date cannot be after end date.");
                return BadRequest(_response);
            }

            try
            {
                // Handle success case
                var stats = await _statRepository.GetTravelAgencyStatsAsync(startDate, endDate);

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = stats;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Handle server error
                _logger.LogError(ex, "An error occurred while fetching travel agency stats for dates: {StartDate} to {EndDate}", startDate, endDate);

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred while processing your request.");
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}