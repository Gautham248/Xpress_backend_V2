using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Sockets;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/travelrequests/{requestId}/ticketoptions")]
    [ApiController]
    public class TicketOptionsController : ControllerBase
    {
        private readonly ITicketOptionServices _ticketOptionService;
        private readonly ITravelRequestServices _travelRequestService;
        private readonly IAuditLogServices _auditLogService;
        private readonly IAuditLogHandlerService _auditLogHandlerService;
        private readonly IMapper _mapper;

        private const int VERIFIED_STATUS_ID = 2;
        private const int OPTIONS_LISTED_STATUS_ID = 3;
        private const int OPTION_SELECTED_STATUS_ID = 4;

        public TicketOptionsController(
            ITicketOptionServices ticketOptionService,
            ITravelRequestServices travelRequestService,
            IAuditLogServices auditLogService,
            IAuditLogHandlerService auditLogHandler,
            IMapper mapper)
        {
            _ticketOptionService = ticketOptionService;
            _travelRequestService = travelRequestService;
            _auditLogService = auditLogService;
            _auditLogHandlerService = auditLogHandler;
            _mapper = mapper;
        }

        private int GetCurrentUserId()
        {
            // Helper to get the authenticated user's ID
            // Ensure your authentication setup populates User.Claims correctly
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        // Fetch Ticket Options by Travel Request
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetTicketOptionsByRequest(string requestId)
        {
            var response = new APIResponse();
            var travelRequestExists = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequestExists == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(response);
            }

            var options = await _ticketOptionService.GetByTravelRequestAsync(requestId);
            response.IsSuccess = true;
            response.Result = _mapper.Map<IEnumerable<TicketOptionResponseDTO>>(options);
            response.StatusCode = HttpStatusCode.OK;
            return Ok(response);
        }

        // Fetch Ticket Options by Option id
        [HttpGet("{optionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> GetTicketOptionById(string requestId, int optionId)
        {
            var response = new APIResponse();
            var option = await _ticketOptionService.GetByIdAsync(optionId);

            if (option == null || option.RequestId != requestId)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Ticket option with ID {optionId} not found for request '{requestId}'.");
                return NotFound(response);
            }

            response.IsSuccess = true;
            response.Result = _mapper.Map<TicketOptionResponseDTO>(option);
            response.StatusCode = HttpStatusCode.OK;
            return Ok(response);
        }

        // Create Ticket Options
        [HttpPost]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> CreateTicketOption(string requestId, [FromBody] CreateTicketOptionDTO createDto)
        {
            var response = new APIResponse();
            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(response);
            }

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(response);
            }

            if (travelRequest.CurrentStatusId != VERIFIED_STATUS_ID && travelRequest.CurrentStatusId != OPTIONS_LISTED_STATUS_ID)
            {   
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Conflict;
                response.ErrorMessages.Add($"Travel request must be in 'Verified' (Status ID: {VERIFIED_STATUS_ID}) or 'OptionsListed' (Status ID: {OPTIONS_LISTED_STATUS_ID}) state to add ticket options. Current status ID: {travelRequest.CurrentStatusId}.");
                return Conflict(response);
            }

            var ticketOption = new TicketOption
            {
                OptionId = Math.Abs(Guid.NewGuid().GetHashCode()),
                RequestId = requestId,
                CreatedByUserId = createDto.CreatedByUserId,
                OptionDescription = createDto.OptionDescription,
                CreatedAt = DateTime.UtcNow,
                IsSelected = false
            };
            await _ticketOptionService.AddAsync(ticketOption);

            var oldStatusId = travelRequest.CurrentStatusId;
            bool statusActuallyChanged = false;

            if (travelRequest.CurrentStatusId == VERIFIED_STATUS_ID)
            {
                travelRequest.CurrentStatusId = OPTIONS_LISTED_STATUS_ID;
                travelRequest.UpdatedAt = DateTime.UtcNow;
                await _travelRequestService.UpdateAsync(travelRequest);
                statusActuallyChanged = true;
            }

            var currentActingUserId = createDto.CreatedByUserId;
            //var auditLogOption = new AuditLog
            //{
            //    RequestId = requestId,
            //    UserId = currentActingUserId,
            //    ActionType = "TICKET_OPTION_CREATED",
            //    ChangeDescription = $"New ticket option {ticketOption.OptionId} ('{ticketOption.OptionDescription}') created.",
            //};
            //await _auditLogService.AddAsync(auditLogOption);

            if (statusActuallyChanged)
            {
                var auditLogStatusChange = new AuditLog
                {
                    RequestId = requestId,
                    UserId = currentActingUserId,
                    ActionType = "STATUS_UPDATED_OPTIONS_LISTED",
                    OldStatusId = oldStatusId,
                    NewStatusId = travelRequest.CurrentStatusId,
                    ChangeDescription = $"Status changed to 'OptionsListed' after first ticket option creation."
                };
                await _auditLogService.AddAsync(auditLogStatusChange);
                await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLogStatusChange);
            }

            var resultDto = _mapper.Map<TicketOptionResponseDTO>(ticketOption);
            response.IsSuccess = true;
            response.Result = resultDto;
            response.StatusCode = HttpStatusCode.Created;
            return CreatedAtAction(nameof(GetTicketOptionById), new { requestId = requestId, optionId = ticketOption.OptionId }, response);
        }

        // Update ticket option
        [HttpPut("{optionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> UpdateTicketOptionDescription(string requestId, int optionId, [FromBody] UpdateTicketOptionDTO updateDto)
        {
            var response = new APIResponse();
            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(response);
            }

            var existingOption = await _ticketOptionService.GetByIdAsync(optionId);
            if (existingOption == null || existingOption.RequestId != requestId)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Ticket option with ID {optionId} not found for request '{requestId}'.");
                return NotFound(response);
            }

            var oldDescription = existingOption.OptionDescription;
            existingOption.OptionDescription = updateDto.OptionDescription;

            await _ticketOptionService.UpdateAsync(existingOption);
            // Audit Log for Ticket Option Edit
            var auditLogEntry = new AuditLog
            {
                RequestId = requestId,
                //UserId = GetCurrentUserId(),
                UserId = 2,
                ActionType = "TICKET_OPTION_EDITED",
                ChangeDescription = $"Ticket option {optionId} description changed from '{oldDescription}' to '{existingOption.OptionDescription}'.",
            };
            await _auditLogService.AddAsync(auditLogEntry);

            var resultDto = _mapper.Map<TicketOptionResponseDTO>(existingOption);
            response.IsSuccess = true;
            response.Result = resultDto;
            response.StatusCode = HttpStatusCode.OK;
            return Ok(response);
        }

        // Select Ticket Option
        [HttpPut("{optionId:int}/select")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> SelectTicketOption(string requestId, int optionId, [FromBody] SelectTicketOptionDTO selectionDto)
        {
            var response = new APIResponse();
            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(response);
            }

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(response);
            }

            if (travelRequest.CurrentStatusId != OPTIONS_LISTED_STATUS_ID && travelRequest.CurrentStatusId != OPTION_SELECTED_STATUS_ID)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Conflict;
                response.ErrorMessages.Add($"Travel request must be in 'OptionsListed' (ID: {OPTIONS_LISTED_STATUS_ID}) or 'OptionSelected' (ID: {OPTION_SELECTED_STATUS_ID}) state to select an option. Current status ID: {travelRequest.CurrentStatusId}.");
                return Conflict(response);
            }

            var optionToSelect = await _ticketOptionService.GetByIdAsync(optionId);
            if (optionToSelect == null || optionToSelect.RequestId != requestId)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Ticket option with ID {optionId} not found for request '{requestId}'.");
                return NotFound(response);
            }

            if (optionToSelect.IsSelected && travelRequest.SelectedTicketOptionId == optionId)
            {
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = $"Ticket option {optionId} is already selected for this request.";
                return Ok(response);
            }

            var allOptionsForRequest = await _ticketOptionService.GetByTravelRequestAsync(requestId);
            foreach (var option in allOptionsForRequest)
            {
                if (option.OptionId != optionId && option.IsSelected)
                {
                    option.IsSelected = false;
                    await _ticketOptionService.UpdateAsync(option);
                }
            }

            optionToSelect.IsSelected = true;
            await _ticketOptionService.UpdateAsync(optionToSelect);

            var oldStatusId = travelRequest.CurrentStatusId;
            travelRequest.SelectedTicketOptionId = optionToSelect.OptionId;
            travelRequest.CurrentStatusId = OPTION_SELECTED_STATUS_ID;
            travelRequest.UpdatedAt = DateTime.UtcNow;
            await _travelRequestService.UpdateAsync(travelRequest);

            var auditLogStatusChange = new AuditLog
            {
                RequestId = requestId,
                UserId = selectionDto.SelectingUserId,
                ActionType = "STATUS_UPDATED_OPTION_SELECTED",
                OldStatusId = oldStatusId,
                NewStatusId = travelRequest.CurrentStatusId,
                ChangeDescription = $"Status set to 'OptionSelected' after ticket option {optionToSelect.OptionId} was selected.",
                Comments = selectionDto.Comments
            };
            await _auditLogService.AddAsync(auditLogStatusChange);
            await _auditLogHandlerService.ProcessAuditLogEntryAsync(auditLogStatusChange);


            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.OK;
            response.Result = new
            {
                Message = $"Option {optionToSelect.OptionId} selected successfully.",
                SelectedOption = _mapper.Map<TicketOptionResponseDTO>(optionToSelect)
            };
            return Ok(response);
        }

        // Delete ticket option
        [HttpDelete("{optionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> DeleteTicketOption(string requestId, int optionId)
        {
            var response = new APIResponse();
            //var currentUserId = GetCurrentUserId();
            var currentUserId = 5;

            var ticketOption = await _ticketOptionService.GetByIdAsync(optionId);
            if (ticketOption == null || ticketOption.RequestId != requestId)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Ticket option with ID {optionId} not found for request '{requestId}'.");
                return NotFound(response);
            }

            bool wasSelected = ticketOption.IsSelected;
            int? oldTravelRequestStatusId = null;
            TravelRequest travelRequest = null;

            if (wasSelected)
            {
                travelRequest = await _travelRequestService.GetByIdAsync(requestId);
                if (travelRequest != null && travelRequest.SelectedTicketOptionId == optionId)
                {
                    oldTravelRequestStatusId = travelRequest.CurrentStatusId;
                    travelRequest.SelectedTicketOptionId = null;
                    // Revert status to "OptionsListed" (3) or "Verified" (2) if no options remain
                    var remainingOptions = (await _ticketOptionService.GetByTravelRequestAsync(requestId)).Count(o => o.OptionId != optionId);
                    if (remainingOptions == 0)
                    {
                        travelRequest.CurrentStatusId = VERIFIED_STATUS_ID;
                    }
                    else
                    {
                        travelRequest.CurrentStatusId = OPTIONS_LISTED_STATUS_ID;
                    }
                    travelRequest.UpdatedAt = DateTime.UtcNow;
                    await _travelRequestService.UpdateAsync(travelRequest);
                }
            }

            try
            {
                await _ticketOptionService.DeleteAsync(optionId);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Failed to delete ticket option {optionId}: {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }

            // Audit Log for Ticket Option Deletion
            var auditLogDeletion = new AuditLog
            {
                RequestId = requestId,
                UserId = currentUserId,
                ActionType = "TICKET_OPTION_DELETED",
                ChangeDescription = $"Ticket option {optionId} ('{ticketOption.OptionDescription}') deleted.",
                Comments = wasSelected ? "This was the previously selected option." : null
            };
            await _auditLogService.AddAsync(auditLogDeletion);

            // Audit Log for Travel Request Status Change (if it happened)
            if (oldTravelRequestStatusId.HasValue && travelRequest != null)
            {
                var auditLogStatusChange = new AuditLog
                {
                    RequestId = requestId,
                    UserId = currentUserId,
                    ActionType = "STATUS_UPDATED_OPTION_DELETED",
                    OldStatusId = oldTravelRequestStatusId.Value, // Should be 4 if selected was deleted
                    NewStatusId = travelRequest.CurrentStatusId,
                    ChangeDescription = $"Status changed after ticket option {optionId} was deleted."
                };
                await _auditLogService.AddAsync(auditLogStatusChange);
            }

            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.OK;
            response.Result = $"Ticket option {optionId} deleted successfully.";
            return Ok(response);
        }

        // Delete all ticket options for a request id
        [HttpDelete("all")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> DeleteAllTicketOptions(string requestId)
        {
            var response = new APIResponse();
            var currentUserId = GetCurrentUserId(); // Implement this to get the actual user
            //var currentUserId = 5; // Placeholder

            var travelRequest = await _travelRequestService.GetByIdAsync(requestId);
            if (travelRequest == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add($"Travel request with ID '{requestId}' not found.");
                return NotFound(response);
            }

            var ticketOptionsToDelete = await _ticketOptionService.GetByTravelRequestAsync(requestId);
            if (ticketOptionsToDelete == null || !ticketOptionsToDelete.Any())
            {
                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = $"No ticket options found to delete for request '{requestId}'.";
                return Ok(response);
            }

            int? oldTravelRequestStatusId = travelRequest.CurrentStatusId;
            bool wasAnyOptionSelected = ticketOptionsToDelete.Any(o => o.IsSelected);
            int selectedOptionIdIfAny = ticketOptionsToDelete.FirstOrDefault(o => o.IsSelected)?.OptionId ?? 0;

            try
            {
                foreach (var option in ticketOptionsToDelete)
                {
                    await _ticketOptionService.DeleteAsync(option.OptionId);
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add($"Failed to delete one or more ticket options for request '{requestId}': {ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }

            bool statusChanged = false;
            if (travelRequest.SelectedTicketOptionId.HasValue && ticketOptionsToDelete.Any(o => o.OptionId == travelRequest.SelectedTicketOptionId.Value))
            {
                travelRequest.SelectedTicketOptionId = null;
                statusChanged = true;
            }

            if (travelRequest.CurrentStatusId != VERIFIED_STATUS_ID)
            {
                travelRequest.CurrentStatusId = VERIFIED_STATUS_ID;
                statusChanged = true;
            }

            if (statusChanged)
            {
                travelRequest.UpdatedAt = DateTime.UtcNow;
                await _travelRequestService.UpdateAsync(travelRequest);
            }

            if (statusChanged && oldTravelRequestStatusId.HasValue)
            {
                var auditLogStatusChange = new AuditLog
                {
                    RequestId = requestId,
                    UserId = currentUserId,
                    ActionType = "STATUS_UPDATED_ALL_OPTIONS_DELETED",
                    OldStatusId = oldTravelRequestStatusId.Value,
                    NewStatusId = travelRequest.CurrentStatusId,
                    ChangeDescription = "Status changed after all ticket options were deleted."
                };
                await _auditLogService.AddAsync(auditLogStatusChange);
            }

            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.OK;
            response.Result = $"All {ticketOptionsToDelete.Count()} ticket options for request '{requestId}' deleted successfully.";
            return Ok(response);
        }
    }
}