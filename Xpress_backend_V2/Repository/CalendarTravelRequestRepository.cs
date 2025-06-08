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
        private readonly string[] _validStatuses = { "PendingReview", "TicketDispatched", "In-transit", "Returned", "Closed" };

        public CalendarTravelRequestRepository(ApiDbContext context)
        {
            _context = context;
        }

        private IQueryable<TravelRequest> GetBaseTravelRequestEntitiesQuery()
        {
            return _context.TravelRequests
                .Include(tr => tr.User) 
                .Include(tr => tr.CurrentStatus) 
                .Where(tr => tr.IsActive &&
                              tr.CurrentStatus != null && 
                              _validStatuses.Contains(tr.CurrentStatus.StatusName));
        }

     
        private CalendarTravelRequestDTO MapToDto(TravelRequest tr)
        {
            if (tr == null) return null;
            return new CalendarTravelRequestDTO
            {
                RequestId = tr.RequestId,
                EmployeeName = tr.User?.EmployeeName, 
                OutboundDepartureDate = tr.OutboundDepartureDate,
                OutboundArrivalDate = tr.OutboundArrivalDate,
                ReturnDepartureDate = tr.ReturnDepartureDate,
                ReturnArrivalDate = tr.ReturnArrivalDate,
                SourcePlace = tr.SourcePlace,
                SourceCountry = tr.SourceCountry,
                DestinationPlace = tr.DestinationPlace,
                DestinationCountry = tr.DestinationCountry,
                CurrentStatusName = tr.CurrentStatus?.StatusName 
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
                    rs.StatusName AS CurrentStatusName
                FROM 
                    TravelRequests tr 
                JOIN 
                    Users u ON tr.UserId = u.UserId 
                JOIN 
                    RequestStatuses rs ON tr.CurrentStatusId = rs.StatusId
                WHERE 
                    tr.IsActive = 1 
                    AND rs.StatusName IN ({statusList}) 
                    AND (
                        -- Only Outbound Departures in date range
                        (CAST(tr.OutboundDepartureDate AS DATE) >= '{startDateStr}' AND CAST(tr.OutboundDepartureDate AS DATE) <= '{endDateStr}') 
                        OR 
                        -- Only Return Arrivals in date range (not outbound arrivals)
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

       
        public async Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsForSpecificDateAsync(DateTime date)
        {
            var query = GetBaseTravelRequestEntitiesQuery()
                .Where(tr =>
                   
                    tr.OutboundDepartureDate.Date == date.Date ||
                  
                    (tr.ReturnArrivalDate.HasValue && tr.ReturnArrivalDate.Value.Date == date.Date)
                );

            var travelRequests = await query.AsNoTracking().ToListAsync();
            return travelRequests.Select(MapToDto);
        }
    }
}