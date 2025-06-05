using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
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
        [ProducesResponseType(typeof(CountDto), 200)]
        public async Task<IActionResult> GetTodaysNewRequestsCount()
        {
            var count = await _statsRepository.GetTodaysNewRequestsCountAsync();
            return Ok(new CountDto { Count = count });
        }

        /// <summary>
        /// Gets the count of today's travel requests that are 'Verified' OR 'DUApproved'.
        /// </summary>
        [HttpGet("count/today/status/verified-or-duapproved")]
        [ProducesResponseType(typeof(CountDto), 200)]
        public async Task<IActionResult> GetTodaysVerifiedOrDuApprovedCount()
        {
            // Using direct string literals
            var statuses = new List<string> { "Verified", "DUApproved" };
            var count = await _statsRepository.GetTodaysRequestsByStatusNamesCountAsync(statuses);
            return Ok(new CountDto { Count = count });
        }

        /// <summary>
        /// API 2 (Clarified): Gets the total count of all travel requests (irrespective of date)
        /// with status 'Verified' OR 'DUApproved'.
        /// </summary>
        [HttpGet("count/all-time/status/verified-or-duapproved")]
        [ProducesResponseType(typeof(CountDto), 200)]
        public async Task<IActionResult> GetAllTimeVerifiedOrDuApprovedCount()
        {
            // Using direct string literals
            var statuses = new List<string> { "Verified", "DUApproved" };
            var count = await _statsRepository.GetRequestsByStatusNamesCountAsync(statuses);
            return Ok(new CountDto { Count = count });
        }

        /// <summary>
        /// API 3: Gets the count of all travel requests created today with status 'Rejected'.
        /// </summary>
        [HttpGet("count/today/status/rejected")]
        [ProducesResponseType(typeof(CountDto), 200)]
        public async Task<IActionResult> GetTodaysRejectedCount()
        {
            // Using direct string literal
            var statuses = new List<string> { "Rejected" };
            var count = await _statsRepository.GetTodaysRequestsByStatusNamesCountAsync(statuses);
            return Ok(new CountDto { Count = count });
        }

        /// <summary>
        /// API 4: Gets the count of outbound departures and return arrivals scheduled for today.
        /// </summary>
        [HttpGet("count/today/travel-legs")]
        [ProducesResponseType(typeof(TravelLegCountsDto), 200)]
        public async Task<IActionResult> GetTodaysTravelLegCounts()
        {
            var counts = await _statsRepository.GetTodaysTravelLegCountsAsync();
            return Ok(counts);
        }

        /// <summary>
        /// API 5: Gets the count of SLA breached requests (status 'Verified' or 'DUApproved',
        /// and time between CreatedAt and UpdatedAt > 24hrs).
        /// </summary>
        [HttpGet("count/sla-breached/verified-or-duapproved")]
        [ProducesResponseType(typeof(CountDto), 200)]
        public async Task<IActionResult> GetSlaBreachedVerifiedOrDuApprovedCount()
        {
            // Using direct string literals
            var statuses = new List<string> { "Verified", "DUApproved" };
            var slaThreshold = TimeSpan.FromHours(24);
            var count = await _statsRepository.GetSlaBreachedRequestsCountAsync(statuses, slaThreshold);
            return Ok(new CountDto { Count = count });
        }
    }
}
