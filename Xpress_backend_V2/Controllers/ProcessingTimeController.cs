using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessingTimeController : ControllerBase
    {
        private readonly IProcessingTimeRepository _processingTimeRepository;

        public ProcessingTimeController(IProcessingTimeRepository processingTimeRepository)
        {
            _processingTimeRepository = processingTimeRepository;
        }

        /// <summary>
        /// Gets the average processing time for travel requests from PendingReview to TicketDispatched.
        /// </summary>
        /// <returns>An object containing the average processing time.</returns>
        [HttpGet("average")]
        [ProducesResponseType(typeof(AverageProcessingTimeDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAverageProcessingTime()
        {
            try
            {
                var (averageTime, requestCount) = await _processingTimeRepository.GetAverageProcessingTimeAsync();

                var dto = new AverageProcessingTimeDto
                {
                    AverageTime = averageTime,
                    FormattedAverageTime = FormatTimeSpan(averageTime),
                    TotalRequestsCalculated = requestCount
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                // In a real app, you would log this exception.
                return StatusCode(500, "An internal server error occurred while calculating processing time.");
            }
        }

        /// <summary>
        /// A helper method to format a TimeSpan into a human-readable string.
        /// </summary>
        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts == TimeSpan.Zero) return "0 Minutes";

            var sb = new StringBuilder();
            if (ts.Days > 0) sb.Append($"{ts.Days} Day{(ts.Days > 1 ? "s" : "")}, ");
            if (ts.Hours > 0) sb.Append($"{ts.Hours} Hour{(ts.Hours > 1 ? "s" : "")}, ");
            if (ts.Minutes > 0) sb.Append($"{ts.Minutes} Minute{(ts.Minutes > 1 ? "s" : "")}");

            var result = sb.ToString().Trim();
            if (result.EndsWith(","))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return string.IsNullOrEmpty(result) ? $"{ts.TotalSeconds:F0} Seconds" : result;
        }
    }
}
