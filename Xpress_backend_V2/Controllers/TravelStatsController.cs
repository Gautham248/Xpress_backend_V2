using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System; 
using System.Collections.Generic; 
using System.Net; 
using System.Threading.Tasks; 
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models; 
using Xpress_backend_V2.Models.DTO; 

namespace Xpress_backend_V2.Controllers
{
    [Route("api/stats/travel-requests")]
    [ApiController]
    public class TravelStatsController : ControllerBase
    {
        private readonly ITravelRequestStatsRepository _statsRepository;

        public TravelStatsController(ITravelRequestStatsRepository statsRepository)
        {
            _statsRepository = statsRepository;
        }

        /// <summary>
        /// API 1: Gets the count of all new travel requests created today.
        /// </summary>
        [HttpGet("count/today/new")]
        [ProducesResponseType(typeof(APIResponse), 200)] // The Result property will be a CountDto
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetTodaysNewRequestsCount()
        {
            var response = new APIResponse();
            try
            {
                var count = await _statsRepository.GetTodaysNewRequestsCountAsync();
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new CountDto { Count = count };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving today's new requests count");
                response.ErrorMessages.Add(ex.Message); // Consider logging ex details instead of returning to client for security
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Gets the count of today's travel requests that are 'Verified' OR 'DUApproved'.
        /// </summary>
        [HttpGet("count/today/status/verified-or-duapproved")]
        [ProducesResponseType(typeof(APIResponse), 200)] // The Result property will be a CountDto
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetTodaysVerifiedOrDuApprovedCount()
        {
            var response = new APIResponse();
            try
            {
                var statuses = new List<string> { "Verified", "DUApproved" };
                var count = await _statsRepository.GetTodaysRequestsByStatusNamesCountAsync(statuses);
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new CountDto { Count = count };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving today's verified or DU approved requests count");
                response.ErrorMessages.Add(ex.Message);
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// API 2 (Clarified): Gets the total count of all travel requests (irrespective of date)
        /// with status 'Verified' OR 'DUApproved'.
        /// </summary>
        [HttpGet("count/all-time/status/verified-or-duapproved")]
        [ProducesResponseType(typeof(APIResponse), 200)] // The Result property will be a CountDto
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetAllTimeVerifiedOrDuApprovedCount()
        {
            var response = new APIResponse();
            try
            {
                var statuses = new List<string> { "Verified", "DUApproved" };
                var count = await _statsRepository.GetRequestsByStatusNamesCountAsync(statuses);
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new CountDto { Count = count };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving all-time verified or DU approved requests count");
                response.ErrorMessages.Add(ex.Message);
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// API 3: Gets the count of all travel requests created today with status 'Rejected'.
        /// </summary>
        [HttpGet("count/today/status/rejected")]
        [ProducesResponseType(typeof(APIResponse), 200)] // The Result property will be a CountDto
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetTodaysRejectedCount()
        {
            var response = new APIResponse();
            try
            {
                var statuses = new List<string> { "Rejected" };
                var count = await _statsRepository.GetTodaysRequestsByStatusNamesCountAsync(statuses);
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new CountDto { Count = count };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving today's rejected requests count");
                response.ErrorMessages.Add(ex.Message);
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// API 4: Gets the count of outbound departures and return arrivals scheduled for today,
        /// including a breakdown by status for statuses: "PendingReview", "Confirmed", "In-transit", "Returned", "Closed".
        /// </summary>
        [HttpGet("count/today/travel-legs")]
        // The Result property of APIResponse will be TravelLegCountsDto, which now includes status breakdowns.
        [ProducesResponseType(typeof(APIResponse), 200)]
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetTodaysTravelLegCounts()
        {
            var response = new APIResponse();
            try
            {
                // This method now returns the TravelLegCountsDto with status-wise breakdowns.
                var counts = await _statsRepository.GetTodaysTravelLegCountsAsync();

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                // The 'counts' object (TravelLegCountsDto) now contains the detailed status counts
                // (TodayOutboundDepartureCountsByStatus and TodayReturnArrivalCountsByStatus).
                response.Result = counts;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving today's travel leg counts");
                response.ErrorMessages.Add(ex.Message);
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// API 5: Gets the count of SLA breached requests (status 'Verified' or 'DUApproved',
        /// and CreatedAt is older than 24 hours from now).
        /// </summary>
        [HttpGet("count/sla-breached/verified-or-duapproved")]
        [ProducesResponseType(typeof(APIResponse), 200)] // The Result property will be a CountDto
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> GetSlaBreachedVerifiedOrDuApprovedCount()
        {
            var response = new APIResponse();
            try
            {
                var statuses = new List<string> { "Verified", "DUApproved" };
                var slaThreshold = TimeSpan.FromHours(24);
                var count = await _statsRepository.GetSlaBreachedRequestsCountAsync(statuses, slaThreshold);
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new CountDto { Count = count };
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("An error occurred while retrieving SLA breached requests count");
                response.ErrorMessages.Add(ex.Message);
                response.Result = null;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }
    }
}