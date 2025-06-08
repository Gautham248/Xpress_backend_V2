using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class TravelRequestRepository : ITravelRequestServices
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<TravelRequestRepository> _logger;
        private readonly IAuditLogServices _auditLogServices;

        public TravelRequestRepository(ApiDbContext context, ILogger<TravelRequestRepository> logger, IAuditLogServices auditLogServices)
        {
            _context = context;
            _logger = logger;
            _auditLogServices = auditLogServices;
        }

        public async Task<IEnumerable<TravelRequest>> GetAllAsync()
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.SelectedTicketOption)
                .Include(tr => tr.BookedAirlines)
                .Where(tr => tr.IsActive)
                .ToListAsync();
        }

        public async Task<TravelRequest> GetByIdAsync(string requestId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.SelectedTicketOption)
                .Include(tr => tr.BookedAirlines)
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId && tr.IsActive);
        }

        public async Task AddAsync(TravelRequest travelRequest)
        {
            travelRequest.CreatedAt = DateTime.UtcNow;
            travelRequest.UpdatedAt = DateTime.UtcNow;
            travelRequest.IsActive = true;
            _context.TravelRequests.Add(travelRequest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TravelRequest travelRequest)
        {
            travelRequest.UpdatedAt = DateTime.UtcNow;
            _context.Entry(travelRequest).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string requestId)
        {
            var travelRequest = await _context.TravelRequests.FindAsync(requestId);
            if (travelRequest != null)
            {
                travelRequest.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TravelRequest>> GetByStatusAsync(int statusId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Where(tr => tr.CurrentStatusId == statusId && tr.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<TravelRequest>> GetByUserAsync(int userId)
        {
            return await _context.TravelRequests
                .Include(tr => tr.User)
                .Include(tr => tr.TravelMode)
                .Include(tr => tr.Project)
                .Include(tr => tr.CurrentStatus)
                .Where(tr => tr.UserId == userId && tr.IsActive)
                .ToListAsync();
        }

        // Travel Info Banner
        public async Task<List<TravelInfoBannerDTO>> GetTravelInfoBannerDetailsAsync(string requestId)
        {
            var query = from tr in _context.TravelRequests
                        join user in _context.Users on tr.UserId equals user.UserId
                        join rmt in _context.RMTs on tr.ProjectCode equals rmt.ProjectCode
                        join mode in _context.TravelModes on tr.TravelModeId equals mode.TravelModeId
                        where tr.RequestId == requestId
                        select new TravelInfoBannerDTO
                        {
                            RequestId = tr.RequestId,
                            EmployeeName = user.EmployeeName,
                            DepartmentName = user.Department,
                            ProjectCode = rmt.ProjectCode,
                            ProjectManager = rmt.ProjectManager,
                            TravelModeName = mode.TravelModeName,
                            SourcePlace = tr.SourcePlace,
                            SourceCountry = tr.SourceCountry,
                            DestinationPlace = tr.DestinationPlace,
                            DestinationCountry = tr.DestinationCountry,
                            PhoneNumber = user.PhoneNumber,
                        };

            return await query.ToListAsync();
        }

        public async Task<TravelRequest> CreateTravelRequestAsync(TravelRequest travelRequest)
        {
            travelRequest.RequestId = Guid.NewGuid().ToString("N");
            travelRequest.CreatedAt = DateTime.UtcNow;
            travelRequest.UpdatedAt = DateTime.UtcNow;
            await _context.TravelRequests.AddAsync(travelRequest);
            await _context.SaveChangesAsync();
            return travelRequest;
        }

        // Travel Info Join Query
        public async Task<List<TravelInfoDTO>> GetTravelInfoAsync(string requestId)
        {
            var query = from tr in _context.TravelRequests
                        join mode in _context.TravelModes on tr.TravelModeId equals mode.TravelModeId
                        where tr.RequestId == requestId
                        select new TravelInfoDTO
                        {
                            RequestId = tr.RequestId,
                            OutboundDepartureDate = tr.OutboundDepartureDate,
                            OutboundArrivalDate = tr.OutboundArrivalDate,
                            ReturnDepartureDate = tr.ReturnDepartureDate,
                            ReturnArrivalDate = tr.ReturnArrivalDate,
                            Transportation = mode.TravelModeName,
                            RequestCreateDate = tr.CreatedAt,
                            PurposeOfTravel = tr.PurposeOfTravel,
                            IsAccommodationRequired = tr.IsAccommodationRequired,
                        };
            return await query.ToListAsync();
        }

        public async Task<TravelRequest> GetTravelRequestByIdAsync(string requestId)
        {
            try
            {
                _logger.LogInformation("Retrieving travel request with ID {RequestId}.", requestId);

                var travelRequest = await _context.TravelRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tr => tr.RequestId == requestId);

                if (travelRequest == null)
                {
                    _logger.LogWarning("Travel request with ID {RequestId} not found.", requestId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved travel request with ID {RequestId}.", requestId);
                return travelRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving travel request with ID {RequestId}.", requestId);
                throw;
            }
        }

        public async Task<TravelRequest> UpdateTravelRequestAsync(TravelRequest travelRequestEntity)
        {
            try
            {
                _logger.LogInformation("Updating travel request with ID {RequestId}.", travelRequestEntity.RequestId);

                // Retrieve the existing travel request
                var existingTravelRequest = await _context.TravelRequests
                    .FirstOrDefaultAsync(tr => tr.RequestId == travelRequestEntity.RequestId);

                if (existingTravelRequest == null)
                {
                    _logger.LogWarning("Travel request with ID {RequestId} not found for update.", travelRequestEntity.RequestId);
                    return null;
                }

                // Update the entity with new values
                _context.Entry(existingTravelRequest).CurrentValues.SetValues(travelRequestEntity);

                // Ensure CreatedAt is not overwritten
                existingTravelRequest.CreatedAt = _context.Entry(existingTravelRequest).OriginalValues.GetValue<DateTime>("CreatedAt");

                // Update the ModifiedAt timestamp
                existingTravelRequest.UpdatedAt = DateTime.UtcNow;

                // Save changes to the database
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated travel request with ID {RequestId}.", travelRequestEntity.RequestId);
                return existingTravelRequest;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating travel request with ID {RequestId}.", travelRequestEntity.RequestId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating travel request with ID {RequestId}.", travelRequestEntity.RequestId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating travel request with ID {RequestId}.", travelRequestEntity.RequestId);
                throw;
            }
        }
        public async Task<TravelRequest> GetRequestByIdAsync(string requestId)
        {
            return await _context.TravelRequests
                .FirstOrDefaultAsync(tr => tr.RequestId == requestId);
        }

        public async Task<TravelRequestTimelineDTO?> GetTimelineAsync(string requestId)
        {
            _logger.LogInformation("Fetching timeline for RequestId: {RequestId}", requestId);

            // Define status-changing action types (uppercase for comparison)
            var statusChangingActions = new[]
            {
                "REQUEST_CREATED",
                "STATUS_UPDATED",
                "MANAGER_APPROVED",
                "VERIFIED",
                "STATUS_UPDATED_OPTIONS_LISTED",
                "STATUS_UPDATED_OPTION_SELECTED",
                "TICKET_OPTION_SELECTED",
                "DU_HEAD_APPROVED",
                "MANAGER_REJECTED",
                "REQUEST_MODIFIED"
            };

            // Fetch audit logs
            var auditLogs = await _auditLogServices.GetByTravelRequestAsync(requestId);
            _logger.LogInformation("Found {Count} audit logs for RequestId: {RequestId}", auditLogs.Count(), requestId);

            // Filter for status-changing actions
            var filteredLogs = auditLogs
                .Where(al => statusChangingActions.Contains(al.ActionType.ToUpper()))
                .OrderBy(al => al.Timestamp)
                .ToList();
            _logger.LogInformation("Filtered to {Count} status-changing logs: {ActionTypes}",
                filteredLogs.Count,
                string.Join(", ", filteredLogs.Select(al => al.ActionType)));

            if (!filteredLogs.Any())
            {
                _logger.LogWarning("No status-changing logs found for RequestId: {RequestId}", requestId);
                return null;
            }

            // Use earliest log for requestDate if REQUEST_CREATED is missing
            var creationLog = filteredLogs.FirstOrDefault(al => al.ActionType.ToUpper() == "REQUEST_CREATED");
            var requestDate = creationLog?.Timestamp ?? filteredLogs.Min(al => al.Timestamp);

            // Get latest status (exclude Modified for overall status)
            var latestLog = filteredLogs
                .Where(al => al.NewStatusId.HasValue && al.NewStatus?.StatusName != "Modified")
                .OrderByDescending(al => al.Timestamp)
                .FirstOrDefault();

            var timelineDto = new TravelRequestTimelineDTO
            {
                Status = latestLog?.NewStatus?.StatusName ?? "PendingReview",
                RequestDate = requestDate.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                TravelerName = "Traveler",
                TimelineEvents = filteredLogs.Select(al => new TimelineEventDTO
                {
                    Id = al.LogId.ToString(),
                    Type = al.ActionType.ToUpper() switch
                    {
                        "REQUEST_CREATED" => "Pending",
                        "REQUEST_MODIFIED" => "Modified",
                        "TICKET_OPTION_SELECTED" => "OptionSelected",
                        _ => al.NewStatus?.StatusName ?? al.ActionType
                    },
                    Date = al.ActionDate == DateTime.MinValue
                        ? al.Timestamp.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
                        : al.ActionDate.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture),
                    Description = al.ChangeDescription ?? "No description provided.",
                    Details = al.Comments
                }).ToList()
            };

            _logger.LogInformation("Returning timeline with {Count} events for RequestId: {RequestId}: {EventTypes}",
                timelineDto.TimelineEvents.Count,
                requestId,
                string.Join(", ", timelineDto.TimelineEvents.Select(e => e.Type)));

            return timelineDto;
        }
    }


}
