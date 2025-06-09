using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Route("api/travelrequests")]
    public class TravelRequestActionsController : ControllerBase
    {
        private readonly ITravelRequestRepo _travelRequestRepo;
        protected APIResponse _response;

        // Define status IDs as constants for clarity and maintainability
        private const int PendingReviewStatusId = 1;
        private const int ModifiedStatusId = 13;

        public TravelRequestActionsController(ITravelRequestRepo travelRequestRepo)
        {
            _travelRequestRepo = travelRequestRepo;
            _response = new APIResponse();
        }

        [HttpPost("{requestId}/edit")]
        [ProducesResponseType(typeof(APIResponse), 200)]
        [ProducesResponseType(typeof(APIResponse), 400)]
        [ProducesResponseType(typeof(APIResponse), 404)]
        [ProducesResponseType(typeof(APIResponse), 500)]
        public async Task<ActionResult<APIResponse>> EditTravelRequest(string requestId, [FromBody] EditTravelRequestDto editDto)
        {
            try
            {
                // We use GetByIdAsync without includes here initially because we only need the base entity
                // for comparison and update. This is slightly more efficient.
                var existingRequest = await _travelRequestRepo.GetByIdAsync(requestId);

                if (existingRequest == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                    return NotFound(_response);
                }

                // Build the string of changes
                var changes = new StringBuilder();
                CompareAndLog(changes, nameof(existingRequest.TravelModeId), existingRequest.TravelModeId, editDto.TravelModeId);
                CompareAndLog(changes, nameof(existingRequest.IsInternational), existingRequest.IsInternational, editDto.IsInternational);
                CompareAndLog(changes, nameof(existingRequest.IsRoundTrip), existingRequest.IsRoundTrip, editDto.IsRoundTrip);
                CompareAndLog(changes, nameof(existingRequest.ProjectCode), existingRequest.ProjectCode, editDto.ProjectCode);
                CompareAndLog(changes, nameof(existingRequest.SourcePlace), existingRequest.SourcePlace, editDto.SourcePlace);
                CompareAndLog(changes, nameof(existingRequest.SourceCountry), existingRequest.SourceCountry, editDto.SourceCountry);
                CompareAndLog(changes, nameof(existingRequest.DestinationPlace), existingRequest.DestinationPlace, editDto.DestinationPlace);
                CompareAndLog(changes, nameof(existingRequest.DestinationCountry), existingRequest.DestinationCountry, editDto.DestinationCountry);
                CompareAndLog(changes, nameof(existingRequest.OutboundDepartureDate), existingRequest.OutboundDepartureDate, editDto.OutboundDepartureDate);
                CompareAndLog(changes, nameof(existingRequest.OutboundArrivalDate), existingRequest.OutboundArrivalDate, editDto.OutboundArrivalDate);
                CompareAndLog(changes, nameof(existingRequest.ReturnDepartureDate), existingRequest.ReturnDepartureDate, editDto.ReturnDepartureDate);
                CompareAndLog(changes, nameof(existingRequest.ReturnArrivalDate), existingRequest.ReturnArrivalDate, editDto.ReturnArrivalDate);
                CompareAndLog(changes, nameof(existingRequest.IsAccommodationRequired), existingRequest.IsAccommodationRequired, editDto.IsAccommodationRequired);
                CompareAndLog(changes, nameof(existingRequest.IsDropOffRequired), existingRequest.IsDropOffRequired, editDto.IsDropOffRequired);
                CompareAndLog(changes, nameof(existingRequest.DropOffPlace), existingRequest.DropOffPlace, editDto.DropOffPlace);
                CompareAndLog(changes, nameof(existingRequest.IsPickUpRequired), existingRequest.IsPickUpRequired, editDto.IsPickUpRequired);
                CompareAndLog(changes, nameof(existingRequest.PickUpPlace), existingRequest.PickUpPlace, editDto.PickUpPlace);
                CompareAndLog(changes, nameof(existingRequest.PurposeOfTravel), existingRequest.PurposeOfTravel, editDto.PurposeOfTravel);
                CompareAndLog(changes, nameof(existingRequest.IsVegetarian), existingRequest.IsVegetarian, editDto.IsVegetarian);
                CompareAndLog(changes, nameof(existingRequest.AttendedCCT), existingRequest.AttendedCCT, editDto.AttendedCCT);

                if (changes.Length == 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages.Add("No changes were detected in the submitted data.");
                    return BadRequest(_response);
                }

                var modificationTime = DateTime.UtcNow;
                var originalStatusId = existingRequest.CurrentStatusId;

                // Step 1: Create the "Modified" log
                var modificationLog = new AuditLog
                {
                    RequestId = requestId,
                    UserId = existingRequest.UserId,
                    ActionType = "Modified",
                    ActionDate = modificationTime,
                    OldStatusId = originalStatusId,
                    NewStatusId = ModifiedStatusId,
                    ChangeDescription = changes.ToString().TrimEnd(),
                    Timestamp = modificationTime
                };
                _travelRequestRepo.AddAuditLog(modificationLog);

                // Step 2: Update the main travel request entity
                existingRequest.TravelModeId = editDto.TravelModeId;
                existingRequest.IsInternational = editDto.IsInternational;
                existingRequest.IsRoundTrip = editDto.IsRoundTrip;
                existingRequest.ProjectCode = editDto.ProjectCode;
                existingRequest.SourcePlace = editDto.SourcePlace;
                existingRequest.SourceCountry = editDto.SourceCountry;
                existingRequest.DestinationPlace = editDto.DestinationPlace;
                existingRequest.DestinationCountry = editDto.DestinationCountry;
                existingRequest.OutboundDepartureDate = editDto.OutboundDepartureDate.ToUniversalTime();
                existingRequest.OutboundArrivalDate = editDto.OutboundArrivalDate?.ToUniversalTime();
                existingRequest.ReturnDepartureDate = editDto.ReturnDepartureDate?.ToUniversalTime();
                existingRequest.ReturnArrivalDate = editDto.ReturnArrivalDate?.ToUniversalTime();
                existingRequest.IsAccommodationRequired = editDto.IsAccommodationRequired;
                existingRequest.IsDropOffRequired = editDto.IsDropOffRequired;
                existingRequest.DropOffPlace = editDto.DropOffPlace;
                existingRequest.IsPickUpRequired = editDto.IsPickUpRequired;
                existingRequest.PickUpPlace = editDto.PickUpPlace;
                existingRequest.Comments = editDto.Comments;
                existingRequest.PurposeOfTravel = editDto.PurposeOfTravel;
                existingRequest.IsVegetarian = editDto.IsVegetarian;
                existingRequest.FoodComment = editDto.FoodComment;
                existingRequest.LDCertificatePath = editDto.LDCertificatePath;
                if (editDto.AttendedCCT.HasValue)
                {
                    existingRequest.AttendedCCT = editDto.AttendedCCT.Value;
                }
                existingRequest.CurrentStatusId = PendingReviewStatusId;
                existingRequest.UpdatedAt = modificationTime;
                _travelRequestRepo.Update(existingRequest);

                // Step 3: Create the "Status Change" log
                var statusChangeLog = new AuditLog
                {
                    RequestId = requestId,
                    UserId = existingRequest.UserId,
                    ActionType = "Status Change",
                    ActionDate = modificationTime,
                    OldStatusId = ModifiedStatusId,
                    NewStatusId = PendingReviewStatusId,
                    ChangeDescription = "Request resubmitted for review after modification.",
                    Timestamp = modificationTime
                };
                _travelRequestRepo.AddAuditLog(statusChangeLog);

                // Step 4: Save all changes in a single database transaction.
                await _travelRequestRepo.SaveChangesAsync();

                // Step 5: Get the final state FROM THE DATABASE for the response.
                // Your repo's GetByIdAsync now uses .Include() to load AuditLogs, resolving the error.
                var updatedRequestWithLogs = await _travelRequestRepo.GetByIdAsync(requestId);

                if (updatedRequestWithLogs == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages.Add("Failed to retrieve the updated request after saving.");
                    return StatusCode(StatusCodes.Status500InternalServerError, _response);
                }

                // Step 6: Map the entity to the "EditedTravelRequestDto" to create a safe response object.
                var responseDto = new EditedTravelRequestDto
                {
                    RequestId = updatedRequestWithLogs.RequestId,
                    UserId = updatedRequestWithLogs.UserId.ToString(),
                    CurrentStatusId = updatedRequestWithLogs.CurrentStatusId,
                    UpdatedAt = updatedRequestWithLogs.UpdatedAt,
                    TravelModeId = updatedRequestWithLogs.TravelModeId,
                    IsInternational = updatedRequestWithLogs.IsInternational,
                    IsRoundTrip = updatedRequestWithLogs.IsRoundTrip,
                    ProjectCode = updatedRequestWithLogs.ProjectCode,
                    SourcePlace = updatedRequestWithLogs.SourcePlace,
                    SourceCountry = updatedRequestWithLogs.SourceCountry,
                    DestinationPlace = updatedRequestWithLogs.DestinationPlace,
                    DestinationCountry = updatedRequestWithLogs.DestinationCountry,
                    OutboundDepartureDate = updatedRequestWithLogs.OutboundDepartureDate,
                    OutboundArrivalDate = updatedRequestWithLogs.OutboundArrivalDate,
                    ReturnDepartureDate = updatedRequestWithLogs.ReturnDepartureDate,
                    ReturnArrivalDate = updatedRequestWithLogs.ReturnArrivalDate,
                    IsAccommodationRequired = updatedRequestWithLogs.IsAccommodationRequired,
                    IsDropOffRequired = updatedRequestWithLogs.IsDropOffRequired,
                    DropOffPlace = updatedRequestWithLogs.DropOffPlace,
                    IsPickUpRequired = updatedRequestWithLogs.IsPickUpRequired,
                    PickUpPlace = updatedRequestWithLogs.PickUpPlace,
                    Comments = updatedRequestWithLogs.Comments,
                    PurposeOfTravel = updatedRequestWithLogs.PurposeOfTravel,
                    IsVegetarian = updatedRequestWithLogs.IsVegetarian,
                    FoodComment = updatedRequestWithLogs.FoodComment,
                    AttendedCCT = updatedRequestWithLogs.AttendedCCT,
                    LDCertificatePath = updatedRequestWithLogs.LDCertificatePath,

                    // Map the collection of audit logs to their DTO counterparts
                    AuditLogs = updatedRequestWithLogs.AuditLogs.Select(log => new EditedRequestAuditLogDto
                    {
                        Id = log.LogId,
                        ActionType = log.ActionType,
                        ActionDate = log.ActionDate,
                        OldStatusId = log.OldStatusId,
                        NewStatusId = log.NewStatusId,
                        ChangeDescription = log.ChangeDescription
                    }).ToList()
                };

                // Populate the success response WITH THE DTO, NOT THE ENTITY
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = responseDto;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add(ex.ToString()); // Use ex.ToString() in development for the full stack trace
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private void CompareAndLog<T>(StringBuilder sb, string fieldName, T oldValue, T newValue)
        {
            if (!object.Equals(oldValue, newValue))
            {
                string oldValStr = (oldValue is DateTime val) ? val.ToString("o") : (oldValue?.ToString() ?? "null");
                string newValStr = (newValue is DateTime newVal) ? newVal.ToString("o") : (newValue?.ToString() ?? "null");
                sb.AppendLine($"{fieldName} changed from '{oldValStr}' to '{newValStr}'.");
            }
        }
    }
}