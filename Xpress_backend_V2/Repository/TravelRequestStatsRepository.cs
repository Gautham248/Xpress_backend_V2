using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;
using System.Linq; // Required for Enumerable.Any and Contains

namespace Xpress_backend_V2.Repository
{
    public class TravelRequestStatsRepository : ITravelRequestStatsRepository
    {
        private readonly ApiDbContext _context;
        // Define the same valid statuses as in CalendarTravelRequestRepository
        private readonly string[] _calendarValidStatuses = { "PendingReview", "TicketDispatched", "In-transit", "Returned", "Closed" };


        public TravelRequestStatsRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTodaysNewRequestsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
           

            return await _context.TravelRequests
                .CountAsync(tr => tr.IsActive && tr.CreatedAt.Date == today);
        }

        public async Task<int> GetTodaysRequestsByStatusNamesCountAsync(IEnumerable<string> statusNames)
        {
            var today = DateTime.UtcNow.Date;
            if (statusNames == null || !statusNames.Any())
            {
                return 0;
            }
            return await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.CreatedAt.Date == today &&
                                   tr.CurrentStatus != null &&
                                   statusNames.Contains(tr.CurrentStatus.StatusName));
        }

        public async Task<int> GetRequestsByStatusNamesCountAsync(IEnumerable<string> statusNames)
        {
            if (statusNames == null || !statusNames.Any())
            {
                return 0;
            }
            return await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.CurrentStatus != null &&
                                   statusNames.Contains(tr.CurrentStatus.StatusName));
        }
        public async Task<TravelLegCountsDto> GetTodaysTravelLegCountsAsync()
        {
            // Get today's date in UTC
            var todayUtc = DateTime.UtcNow.Date;

            var outboundDepartures = await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.OutboundDepartureDate.Date == todayUtc &&
                                   tr.CurrentStatus != null &&
                                   _calendarValidStatuses.Contains(tr.CurrentStatus.StatusName));

            var returnArrivals = await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.ReturnArrivalDate.HasValue &&
                                   tr.ReturnArrivalDate.Value.Date == todayUtc &&
                                   tr.CurrentStatus != null &&
                                   _calendarValidStatuses.Contains(tr.CurrentStatus.StatusName));

            return new TravelLegCountsDto
            {
                TodayOutboundDepartureCount = outboundDepartures,
                TodayReturnArrivalCount = returnArrivals,
            };
        }
        public async Task<int> GetSlaBreachedRequestsCountAsync(IEnumerable<string> statusNames, TimeSpan slaThreshold)
        {
            if (statusNames == null || !statusNames.Any())
            {
                return 0;
            }

            var currentTime = DateTime.UtcNow;
            var slaBreachCutoff = currentTime.Subtract(slaThreshold);

            return await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.CurrentStatus != null &&
                                   statusNames.Contains(tr.CurrentStatus.StatusName) &&
                                   tr.CreatedAt < slaBreachCutoff);
        }
    }
}