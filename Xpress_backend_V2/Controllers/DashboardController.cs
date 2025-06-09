using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;
using Xpress_backend_V2.Models.DTO;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Interface; 

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _dashboardRepository;
        private const int MaxDateRangeDays = 180; // Set a reasonable max range (e.g., 6 months)

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        // =================================================================================
        // API 1: STATUS OVERVIEW
        // =================================================================================
        [HttpGet("status-overview")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetStatusOverview([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = new APIResponse();
            try
            {
                // --- Input Validation ---
                if (ValidateDateRange(startDate, endDate, response) is var validationResult && validationResult != null)
                {
                    return validationResult;
                }

               
                var data = await _dashboardRepository.GetRequestStatusOverviewAsync(startDate, endDate);
                response.Result = data;
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add($"An unexpected error occurred: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                // Consider logging the full exception `ex` here
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        // =================================================================================
        // API 2: EXPENSE OVERVIEW
        // =================================================================================
        [HttpGet("expense-overview")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetExpenseOverview([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = new APIResponse();
            try
            {
                // --- Input Validation ---
                if (ValidateDateRange(startDate, endDate, response) is var validationResult && validationResult != null)
                {
                    return validationResult;
                }

               
                var data = await _dashboardRepository.GetExpenseOverviewAsync(startDate, endDate);
                response.Result = data;
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add($"An unexpected error occurred: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        // =================================================================================
        // API 3: TRIP DETAILS OVERVIEW
        // =================================================================================
        [HttpGet("trip-details")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetTripDetailsOverview([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = new APIResponse();
            try
            {
                // --- Input Validation ---
                if (ValidateDateRange(startDate, endDate, response) is var validationResult && validationResult != null)
                {
                    return validationResult;
                }

                
                var data = await _dashboardRepository.GetTripDetailsOverviewAsync(startDate, endDate);
                response.Result = data;
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add($"An unexpected error occurred: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        // =================================================================================
        // Reusable Validation Method
        // =================================================================================
        private BadRequestObjectResult ValidateDateRange(DateTime startDate, DateTime endDate, APIResponse response)
        {
            if (startDate == default || endDate == default)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("Both startDate and endDate query parameters are required.");
                return BadRequest(response);
            }

            if (endDate < startDate)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add("End date cannot be earlier than the start date.");
                return BadRequest(response);
            }

            if (endDate - startDate > TimeSpan.FromDays(MaxDateRangeDays))
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add($"The date range cannot exceed {MaxDateRangeDays} days.");
                return BadRequest(response);
            }

        
            return null;
        }
    }
}