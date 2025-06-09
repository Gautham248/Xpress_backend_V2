using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingTimeController : ControllerBase
    {
        private readonly IProcessingTimeRepository _processingTimeRepository;

        public ProcessingTimeController(IProcessingTimeRepository processingTimeRepository)
        {
            _processingTimeRepository = processingTimeRepository;
        }

        /// <summary>
        /// Calculates the average time for a request from 'PendingReview' to 'TicketDispatched'.
        /// </summary>
        /// <returns>An APIResponse containing the average time statistics.</returns>
        [HttpGet("average-review-to-dispatch")]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetAverageProcessingTime()
        {
            var response = new APIResponse();
            try
            {
                
                var data = await _processingTimeRepository.GetAverageReviewToDispatchTimeAsync();

                
                if (data == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Required status types for calculation ('PendingReview' or 'TicketDispatched') were not found in the database.");
                    return NotFound(response);
                }

                // --- Success Case ---
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
    }
}
