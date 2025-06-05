using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;
using Dapper;
using Xpress_backend_V2.Models;
namespace Xpress_backend_V2.Repository
{
    public class CalendarTravelRequestRepository : ICalendarTravelRequestRepository
    {
        private readonly ApiDbContext _context;
        private readonly string[] _validStatuses = { "PendingReview", "Tickets Dispatched", "In-transit", "Returned", "Closed" };

        public CalendarTravelRequestRepository(ApiDbContext context)
        {
            _context = context;
        }

        // This method now returns IQueryable<TravelRequest> because the Select to DTO
        // will be done after specific filters are applied.
        // This is slightly less efficient than projecting earlier if many fields are not needed,
        // but simplifies applying different filters before the final Select
        // Alternatively, keep it as IQueryable<CalendarTravelRequestDTO> and re-apply Select in each method,
        // or pass the Select expression. For simplicity now, we select later.
        private IQueryable<TravelRequest> GetBaseTravelRequestEntitiesQuery()
        {
            return _context.TravelRequests
                .Include(tr => tr.User) // To get EmployeeName
                .Include(tr => tr.CurrentStatus) // To get CurrentStatusName (now StatusName)
                .Where(tr => tr.IsActive &&
                              tr.CurrentStatus != null && // Ensure CurrentStatus is loaded
                              _validStatuses.Contains(tr.CurrentStatus.StatusName)); // Use StatusName from your RequestStatus model
        }

        // Helper to convert TravelRequest to DTO
        private CalendarTravelRequestDTO MapToDto(TravelRequest tr)
        {
            if (tr == null) return null;
            return new CalendarTravelRequestDTO
            {
                RequestId = tr.RequestId,
                EmployeeName = tr.User?.EmployeeName, // Null conditional for safety
                OutboundDepartureDate = tr.OutboundDepartureDate,
                OutboundArrivalDate = tr.OutboundArrivalDate,
                ReturnDepartureDate = tr.ReturnDepartureDate,
                ReturnArrivalDate = tr.ReturnArrivalDate,
                SourcePlace = tr.SourcePlace,
                SourceCountry = tr.SourceCountry,
                DestinationPlace = tr.DestinationPlace,
                DestinationCountry = tr.DestinationCountry,
                CurrentStatusName = tr.CurrentStatus?.StatusName // Use StatusName and null conditional
            };
        }


        public async Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsAsync()
        {
            var travelRequests = await GetBaseTravelRequestEntitiesQuery()
                                        .AsNoTracking()
                                        .ToListAsync();
            return travelRequests.Select(MapToDto);
        }

        public async Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByRangeAsync(DateTime startDate, DateTime endDate)
        {
            var query = GetBaseTravelRequestEntitiesQuery()
                .Where(tr =>
                    (tr.OutboundDepartureDate.Date >= startDate.Date && tr.OutboundDepartureDate.Date <= endDate.Date) ||
                    (tr.ReturnArrivalDate.HasValue && tr.ReturnArrivalDate.Value.Date >= startDate.Date && tr.ReturnArrivalDate.Value.Date <= endDate.Date)
                );

            var travelRequests = await query.AsNoTracking().ToListAsync();
            return travelRequests.Select(MapToDto);
        }

        public async Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByRangeOptimizedAsync(DateTime startDate, DateTime endDate)
        {
            var statusList = string.Join(",", _validStatuses.Select(s => $"'{s}'"));
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            // IMPORTANT: Adjust table names (TravelRequests, Users, RequestStatuses)
            // and column names (u.EmployeeName, rs.StatusName, u.UserId, rs.StatusId)
            // in the SQL query below to match your actual database schema.
            var sql = $@"
                SELECT 
                    tr.RequestId, 
                    u.EmployeeName AS EmployeeName, 
                    tr.OutboundDepartureDate, 
                    tr.OutboundArrivalDate, 
                    tr.ReturnDepartureDate, 
                    tr.ReturnArrivalDate, 
                    tr.SourcePlace, 
                    tr.SourceCountry, 
                    tr.DestinationPlace, 
                    tr.DestinationCountry, 
                    rs.StatusName AS CurrentStatusName -- Use StatusName from RequestStatus table
                FROM 
                    TravelRequests tr 
                JOIN 
                    Users u ON tr.UserId = u.UserId 
                JOIN 
                    RequestStatuses rs ON tr.CurrentStatusId = rs.StatusId -- Join on StatusId (PK of RequestStatus)
                WHERE 
                    tr.IsActive = 1 
                    AND rs.StatusName IN ({statusList}) 
                    AND (
                        (CAST(tr.OutboundDepartureDate AS DATE) >= '{startDateStr}' AND CAST(tr.OutboundDepartureDate AS DATE) <= '{endDateStr}') 
                        OR 
                        (tr.ReturnArrivalDate IS NOT NULL AND CAST(tr.ReturnArrivalDate AS DATE) >= '{startDateStr}' AND CAST(tr.ReturnArrivalDate AS DATE) <= '{endDateStr}')
                    )";

            var connection = _context.Database.GetDbConnection();
            return await connection.QueryAsync<CalendarTravelRequestDTO>(sql, new { startDate = startDateStr, endDate = endDateStr });
        }

        public async Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByTypeAndRangeAsync(DateTime startDate, DateTime endDate, string eventType)
        {
            var baseQuery = GetBaseTravelRequestEntitiesQuery();
            IQueryable<TravelRequest> query;

            switch (eventType)
            {
                case "OutboundDeparture":
                    query = baseQuery.Where(tr => tr.OutboundDepartureDate.Date >= startDate.Date &&
                                              tr.OutboundDepartureDate.Date <= endDate.Date);
                    break;
                case "ReturnArrival":
                    query = baseQuery.Where(tr => tr.ReturnArrivalDate.HasValue &&
                                              tr.ReturnArrivalDate.Value.Date >= startDate.Date &&
                                              tr.ReturnArrivalDate.Value.Date <= endDate.Date);
                    break;
                default:
                    return Enumerable.Empty<CalendarTravelRequestDTO>();
            }

            var travelRequests = await query.AsNoTracking().ToListAsync();
            return travelRequests.Select(MapToDto);
        }
    }
}
