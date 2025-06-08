using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net; 
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;     
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirlineReportsController : ControllerBase
    {
        private readonly IAirlineReportRepository _airlineReportRepository;
        private readonly ILogger<AirlineReportsController> _logger;
        
        protected APIResponse _response;
        
        public AirlineReportsController(IAirlineReportRepository airlineReportRepository, ILogger<AirlineReportsController> logger)
        {
            _airlineReportRepository = airlineReportRepository;
            _logger = logger;
           
            this._response = new APIResponse();
        }

        [HttpGet]
        
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
       
        public async Task<ActionResult<APIResponse>> GetAirlineReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
           
            if (startDate > endDate)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("The start date cannot be after the end date.");
                return BadRequest(_response);
            }

            try
            {
              
                var report = await _airlineReportRepository.GetAirlineReportAsync(startDate, endDate);

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = report;

                return Ok(_response);
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "An error occurred while generating the airline report for dates {StartDate} to {EndDate}", startDate, endDate);

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add("An internal server error occurred. Please try again later.");

                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}