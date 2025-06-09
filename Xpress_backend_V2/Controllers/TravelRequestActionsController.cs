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

        public TravelRequestActionsController(ITravelRequestRepo travelRequestRepo)
        {
            _travelRequestRepo = travelRequestRepo;
        }

        [HttpPost("{requestId}/edit")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> EditTravelRequest(string requestId, [FromBody] EditTravelRequestDto editDto)
        {
            var existingRequest = await _travelRequestRepo.GetByIdAsync(requestId);

            if (existingRequest == null)
            {
                return NotFound($"Travel request with ID '{requestId}' not found.");
            }

            var changes = new StringBuilder();

            CompareAndLog(changes, "Travel Mode", existingRequest.TravelModeId, editDto.TravelModeId);
            CompareAndLog(changes, "Is International", existingRequest.IsInternational, editDto.IsInternational);
            CompareAndLog(changes, "Is Round Trip", existingRequest.IsRoundTrip, editDto.IsRoundTrip);
            CompareAndLog(changes, "Project Code", existingRequest.ProjectCode, editDto.ProjectCode);
            CompareAndLog(changes, "Source Place", existingRequest.SourcePlace, editDto.SourcePlace);
            CompareAndLog(changes, "Source Country", existingRequest.SourceCountry, editDto.SourceCountry);
            CompareAndLog(changes, "Destination Place", existingRequest.DestinationPlace, editDto.DestinationPlace);
            CompareAndLog(changes, "Destination Country", existingRequest.DestinationCountry, editDto.DestinationCountry);
            CompareAndLog(changes, "Outbound Departure", existingRequest.OutboundDepartureDate, editDto.OutboundDepartureDate);
            CompareAndLog(changes, "Outbound Arrival", existingRequest.OutboundArrivalDate, editDto.OutboundArrivalDate);
            CompareAndLog(changes, "Return Departure", existingRequest.ReturnDepartureDate, editDto.ReturnDepartureDate);
            CompareAndLog(changes, "Return Arrival", existingRequest.ReturnArrivalDate, editDto.ReturnArrivalDate);
            CompareAndLog(changes, "Accommodation Required", existingRequest.IsAccommodationRequired, editDto.IsAccommodationRequired);
            CompareAndLog(changes, "Drop-off Required", existingRequest.IsDropOffRequired, editDto.IsDropOffRequired);
            CompareAndLog(changes, "Drop-off Place", existingRequest.DropOffPlace, editDto.DropOffPlace);
            CompareAndLog(changes, "Pick-up Required", existingRequest.IsPickUpRequired, editDto.IsPickUpRequired);
            CompareAndLog(changes, "Pick-up Place", existingRequest.PickUpPlace, editDto.PickUpPlace);
            CompareAndLog(changes, "Purpose of Travel", existingRequest.PurposeOfTravel, editDto.PurposeOfTravel);
            CompareAndLog(changes, "Is Vegetarian", existingRequest.IsVegetarian, editDto.IsVegetarian);
            CompareAndLog(changes, "Attended CCT", existingRequest.AttendedCCT, editDto.AttendedCCT);

            if (changes.Length == 0)
            {
                return BadRequest("No changes were detected in the submitted data.");
            }

            var originalStatusId = existingRequest.CurrentStatusId;
            var modificationTime = DateTime.UtcNow;

            var modificationLog = new AuditLog
            {
                RequestId = requestId,
                UserId = existingRequest.UserId,
                ActionType = "Modified",
                ActionDate = modificationTime,
                OldStatusId = originalStatusId,
                NewStatusId = 13, // Status: Modified
                ChangeDescription = changes.ToString().TrimEnd(),
                Timestamp = modificationTime
            };
            _travelRequestRepo.AddAuditLog(modificationLog);

            // Update the existing entity
            existingRequest.TravelModeId = editDto.TravelModeId;
            existingRequest.IsInternational = editDto.IsInternational;
            existingRequest.IsRoundTrip = editDto.IsRoundTrip;
            existingRequest.ProjectCode = editDto.ProjectCode;
            existingRequest.SourcePlace = editDto.SourcePlace;
            existingRequest.SourceCountry = editDto.SourceCountry;
            existingRequest.DestinationPlace = editDto.DestinationPlace;
            existingRequest.DestinationCountry = editDto.DestinationCountry;
            existingRequest.OutboundDepartureDate = editDto.OutboundDepartureDate.ToUniversalTime();
            existingRequest.OutboundArrivalDate = editDto.OutboundArrivalDate.ToUniversalTime();
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

            existingRequest.CurrentStatusId = 1; // Status: PendingReview
            existingRequest.UpdatedAt = modificationTime;

            _travelRequestRepo.Update(existingRequest);

            var statusChangeLog = new AuditLog
            {
                RequestId = requestId,
                UserId = existingRequest.UserId,
                ActionType = "Status Change",
                ActionDate = modificationTime,
                OldStatusId = 13, // Status: Modified
                NewStatusId = 1,  // Status: PendingReview
                ChangeDescription = "Request resubmitted for review after modification.",
                Timestamp = modificationTime
            };
            _travelRequestRepo.AddAuditLog(statusChangeLog);

            await _travelRequestRepo.SaveChangesAsync();

            return NoContent();
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
