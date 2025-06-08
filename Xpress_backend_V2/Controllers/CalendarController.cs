using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarTravelRequestRepository _calendarRepository;
        private const int MaxDateRangeDays = 90; 
        protected APIResponse _response;

        public CalendarController(ICalendarTravelRequestRepository calendarRepository)
        {
            _calendarRepository = calendarRepository;
            _response = new APIResponse();
        }

        [HttpGet("events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCalendarEvents()
        {
            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsAsync();

                _response.IsSuccess = true;
                _response.Result = travelRequests;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEvents: {ex.Message}"); // Basic logging 

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred while retrieving calendar events.");
                return StatusCode(500, _response);
            }
        }

       
        /// Gets calendar travel request events within a specified date range.
        /// Checks OutboundDepartureDate and ReturnArrivalDate.
        
        [HttpGet("events/range")] 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCalendarEventsByRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Both startDate and endDate query parameters are required.");
                return BadRequest(_response);
            }

            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("The startDate cannot be later than the endDate.");
                return BadRequest(_response);
            }

            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"The date range cannot exceed {MaxDateRangeDays} days.");
                return BadRequest(_response);
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByRangeAsync(startDate, endDate);

                _response.IsSuccess = true;
                _response.Result = travelRequests;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByRange: {ex.Message}");

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred while retrieving calendar events by range.");
                return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Gets calendar travel request events within a specified date range using an optimized query.
        /// Checks OutboundDepartureDate and ReturnArrivalDate.
        /// </summary>
        [HttpGet("events/range-optimized")] 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCalendarEventsByRangeOptimized(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Both startDate and endDate query parameters are required.");
                return BadRequest(_response);
            }

            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("The startDate cannot be later than the endDate.");
                return BadRequest(_response);
            }

            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"The date range cannot exceed {MaxDateRangeDays} days.");
                return BadRequest(_response);
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByRangeOptimizedAsync(startDate, endDate);

                _response.IsSuccess = true;
                _response.Result = travelRequests;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByRangeOptimized: {ex.Message}");

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred while retrieving optimized calendar events by range.");
                return StatusCode(500, _response);
            }
        }

        /// <summary>
        /// Gets calendar travel request events of a specific type (OutboundDeparture or ReturnArrival) within a date range.
        /// </summary>
        [HttpGet("events/range-by-type")] // e.g., GET api/calendar/events/range-by-type?startDate=...&endDate=...&eventType=OutboundDeparture
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetCalendarEventsByTypeAndRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string eventType)
        {
            if (startDate == default || endDate == default)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Both startDate and endDate query parameters are required.");
                return BadRequest(_response);
            }

            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("The startDate cannot be later than the endDate.");
                return BadRequest(_response);
            }

            var validEventTypes = new[] { "OutboundDeparture", "ReturnArrival" };
            if (string.IsNullOrWhiteSpace(eventType) || !validEventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"The eventType query parameter is required and must be one of: {string.Join(", ", validEventTypes)}.");
                return BadRequest(_response);
            }

            var normalizedEventType = validEventTypes.First(vet => vet.Equals(eventType, StringComparison.OrdinalIgnoreCase));

            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add($"The date range cannot exceed {MaxDateRangeDays} days.");
                return BadRequest(_response);
            }

            try
            {
                var travelRequests = await _calendarRepository.GetCalendarEventsByTypeAndRangeAsync(startDate, endDate, normalizedEventType);

                _response.IsSuccess = true;
                _response.Result = travelRequests;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCalendarEventsByTypeAndRange: {ex.Message}");

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An unexpected error occurred while retrieving calendar events by type and range.");
                return StatusCode(500, _response);
            }
        }
    }
}