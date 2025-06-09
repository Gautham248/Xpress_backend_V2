
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/document-status")]
    [ApiController]
    public class DocumentStatusController : ControllerBase
    {
        private readonly IDocumentStatusRepository _documentStatusRepository;
        private const int MaxDateRangeDays = 180;

        public DocumentStatusController(IDocumentStatusRepository documentStatusRepository)
        {
            _documentStatusRepository = documentStatusRepository;
        }

        [HttpGet("passport")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetPassportStatus([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = new APIResponse();
            try
            {
                if (ValidateDateRange(startDate, endDate, response) is var validationResult && validationResult != null)
                {
                    return validationResult;
                }

                var data = await _documentStatusRepository.GetPassportStatusAsync(endDate);

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

        [HttpGet("visa")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetVisaStatus([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = new APIResponse();
            try
            {
                if (ValidateDateRange(startDate, endDate, response) is var validationResult && validationResult != null)
                {
                    return validationResult;
                }

                var data = await _documentStatusRepository.GetVisaStatusAsync(endDate);

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