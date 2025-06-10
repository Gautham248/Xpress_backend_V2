using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogServices _auditLogService;
        private readonly ITravelRequestServices _travelRequestService;
        private readonly ApiDbContext _context;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public AuditLogsController(IAuditLogServices auditLogService, ITravelRequestServices travelRequestService, ApiDbContext context, IMapper mapper)
        {
            _auditLogService = auditLogService;
            _travelRequestService = travelRequestService;
            _context = context;
            _response = new APIResponse();
            _mapper = mapper;
        }

        // Get Audit Log by Log Id
        [HttpGet("{logId:int}", Name = "GetAuditLogById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetAuditLogById(int logId)
        {
            if (logId <= 0)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Invalid Log ID.");
                return BadRequest(_response);
            }

            var auditLog = await _auditLogService.GetByIdAsync(logId);

            if (auditLog == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Audit log with ID {logId} not found.");
                return NotFound(_response);
            }

            var auditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLog);

            _response.IsSuccess = true;
            _response.Result = auditLogDto;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        // Get Audit Log by Request Id
        [HttpGet("{requestId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> GetAuditLogsByTravelRequest(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Travel Request ID cannot be empty.");
                return BadRequest(_response);
            }

            var auditLogs = await _auditLogService.GetByTravelRequestAsync(requestId);

            var auditLogDtos = _mapper.Map<List<AuditLogResponseDTO>>(auditLogs);

            _response.IsSuccess = true;
            _response.Result = auditLogDtos;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }


        // Create Audit Log Event
        [HttpPost("logevent/{requestId}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> LogAuditEvent(
    [FromBody] AuditLogDTO auditLogDto)
        {
            if (!ModelState.IsValid)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(_response);
            }

            
            if (string.IsNullOrWhiteSpace(auditLogDto.RequestId))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Request ID cannot be empty.");
                return BadRequest(_response);
            }

            
            var travelRequest = await _travelRequestService.GetByIdAsync(auditLogDto.RequestId);
            if (travelRequest == null)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add($"Travel Request with ID {auditLogDto.RequestId} not found.");
                return NotFound(_response);
            }

            
            var auditLogEntry = _mapper.Map<AuditLog>(auditLogDto);

            
            if (!string.IsNullOrWhiteSpace(auditLogDto.CustomChangeDescription))
            {
                auditLogEntry.ChangeDescription = auditLogDto.CustomChangeDescription;
            }
            else
            {
                if (auditLogDto.ActionType.Equals("STATUS_UPDATED", StringComparison.OrdinalIgnoreCase) &&
                    auditLogDto.OldStatusId.HasValue && auditLogDto.NewStatusId.HasValue)
                {
                    var oldStatusName = (await _context.RequestStatuses.FindAsync(auditLogDto.OldStatusId.Value))?.StatusName ?? auditLogDto.OldStatusId.Value.ToString();
                    var newStatusName = (await _context.RequestStatuses.FindAsync(auditLogDto.NewStatusId.Value))?.StatusName ?? auditLogDto.NewStatusId.Value.ToString();
                    auditLogEntry.ChangeDescription = $"Status changed from '{oldStatusName}' to '{newStatusName}'.";
                }
                else if (auditLogDto.ActionType.Equals("REQUEST_CREATED", StringComparison.OrdinalIgnoreCase))
                {
                    auditLogEntry.ChangeDescription = "Travel request created.";
                }
                else
                {
                    auditLogEntry.ChangeDescription = $"Action '{auditLogDto.ActionType}' performed.";
                }
            }

            await _auditLogService.AddAsync(auditLogEntry);

            var createdAuditLogDto = _mapper.Map<AuditLogResponseDTO>(auditLogEntry);

            _response.IsSuccess = true;
            _response.Result = createdAuditLogDto;
            _response.StatusCode = HttpStatusCode.Created;
            return CreatedAtRoute("GetAuditLogById", new { logId = auditLogEntry.LogId }, _response);
        }



        

    }
}

