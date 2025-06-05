using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/calendar")] // Using a clear base route like "api/calendar"
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarTravelRequestRepository _calendarRepository;
        private const int MaxDateRangeDays = 90; // Consistent date range limit

        public CalendarController(ICalendarTravelRequestRepository calendarRepository)
        {
            _calendarRepository = calendarRepository;
        }

        /// <summary>
        /// Gets all relevant travel request events for the calendar.
        /// </summary>
        [HttpGet("events")] // e.g., GET api/calendar/events
        public async Task<ActionResult<IEnumerable<CalendarTravelRequestDTO>>> GetCalendarEvents()
        {
            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsAsync();
                return Ok(travelRequests);
            }
            catch (Exception ex)
            {
                // It's good practice to log the exception details (ex) using a logging framework
                Console.WriteLine($"Error in GetCalendarEvents: {ex.Message}"); // Basic logging for now
                return StatusCode(500, "An unexpected error occurred while retrieving calendar events.");
            }
        }

        /// <summary>
        /// Gets calendar travel request events within a specified date range.
        /// Checks OutboundDepartureDate and ReturnArrivalDate.
        /// </summary>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        [HttpGet("events/range")] // e.g., GET api/calendar/events/range?startDate=...&endDate=...
        public async Task<ActionResult<IEnumerable<CalendarTravelRequestDTO>>> GetCalendarEventsByRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest("Both startDate and endDate query parameters are required.");
            }
            if (startDate > endDate)
            {
                return BadRequest("The startDate cannot be later than the endDate.");
            }
            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                return BadRequest($"The date range cannot exceed {MaxDateRangeDays} days.");
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByRangeAsync(startDate, endDate);
                return Ok(travelRequests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByRange: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while retrieving calendar events by range.");
            }
        }

        /// <summary>
        /// Gets calendar travel request events within a specified date range using an optimized query.
        /// Checks OutboundDepartureDate and ReturnArrivalDate.
        /// </summary>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        [HttpGet("events/range-optimized")] // e.g., GET api/calendar/events/range-optimized?startDate=...&endDate=...
        public async Task<ActionResult<IEnumerable<CalendarTravelRequestDTO>>> GetCalendarEventsByRangeOptimized(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest("Both startDate and endDate query parameters are required.");
            }
            if (startDate > endDate)
            {
                return BadRequest("The startDate cannot be later than the endDate.");
            }
            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                return BadRequest($"The date range cannot exceed {MaxDateRangeDays} days.");
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByRangeOptimizedAsync(startDate, endDate);
                return Ok(travelRequests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByRangeOptimized: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while retrieving optimized calendar events by range.");
            }
        }

        /// <summary>
        /// Gets calendar travel request events of a specific type (OutboundDeparture or ReturnArrival) within a date range.
        /// </summary>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <param name="eventType">The type of event ("OutboundDeparture" or "ReturnArrival").</param>
        [HttpGet("events/range-by-type")] // e.g., GET api/calendar/events/range-by-type?startDate=...&endDate=...&eventType=OutboundDeparture
        public async Task<ActionResult<IEnumerable<CalendarTravelRequestDTO>>> GetCalendarEventsByTypeAndRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string eventType)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest("Both startDate and endDate query parameters are required.");
            }
            if (startDate > endDate)
            {
                return BadRequest("The startDate cannot be later than the endDate.");
            }

            var validEventTypes = new[] { "OutboundDeparture", "ReturnArrival" };
            if (string.IsNullOrWhiteSpace(eventType) || !validEventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest($"The eventType query parameter is required and must be one of: {string.Join(", ", validEventTypes)}.");
            }

            // Normalize eventType to match the casing used in the repository if needed,
            // though the repository switch statement should handle various casings if written carefully.
            // For consistency, find the exact match from validEventTypes:
            var normalizedEventType = validEventTypes.First(vet => vet.Equals(eventType, StringComparison.OrdinalIgnoreCase));


            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                return BadRequest($"The date range cannot exceed {MaxDateRangeDays} days.");
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByTypeAndRangeAsync(startDate, endDate, normalizedEventType);
                return Ok(travelRequests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByTypeAndRange: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred while retrieving calendar events by type and range.");
            }
        }
    }
}
