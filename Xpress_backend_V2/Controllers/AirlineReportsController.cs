using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net; // Make sure this using statement is present for HttpStatusCode
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;     // Assuming APIResponse is in this namespace
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirlineReportsController : ControllerBase
    {
        private readonly IAirlineReportRepository _airlineReportRepository;
        private readonly ILogger<AirlineReportsController> _logger;
        // The standardized response object, as per your example
        protected APIResponse _response;

        public AirlineReportsController(IAirlineReportRepository airlineReportRepository, ILogger<AirlineReportsController> logger)
        {
            _airlineReportRepository = airlineReportRepository;
            _logger = logger;
            // Initialize the response object for each request
            this._response = new APIResponse();
        }

        [HttpGet]
        // Update the ProducesResponseType to reflect the APIResponse wrapper
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        // The return type is now ActionResult<APIResponse>
        public async Task<ActionResult<APIResponse>> GetAirlineReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            // Handle validation error
            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("The start date cannot be after the end date.");
                return BadRequest(_response);
            }

            try
            {
                // Main logic for success case
                var report = await _airlineReportRepository.GetAirlineReportAsync(startDate, endDate);

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = report;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Handle server error
                _logger.LogError(ex, "An error occurred while generating the airline report for dates {StartDate} to {EndDate}", startDate, endDate);

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An internal server error occurred. Please try again later.");

                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}