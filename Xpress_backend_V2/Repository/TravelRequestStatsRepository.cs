using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class TravelRequestStatsRepository : ITravelRequestStatsRepository
    {
        private readonly ApiDbContext _context; // Replace with your actual DbContext name

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
            var today = DateTime.UtcNow.Date;

            var outboundDepartures = await _context.TravelRequests
                .CountAsync(tr => tr.IsActive && tr.OutboundDepartureDate.Date == today);

            var returnArrivals = await _context.TravelRequests
                .CountAsync(tr => tr.IsActive &&
                                   tr.ReturnArrivalDate.HasValue &&
                                   tr.ReturnArrivalDate.Value.Date == today);

            return new TravelLegCountsDto
            {
                TodayOutboundDepartureCount = outboundDepartures,
                TodayReturnArrivalCount = returnArrivals
            };
        }

        public async Task<int> GetSlaBreachedRequestsCountAsync(IEnumerable<string> statusNames, TimeSpan slaThreshold)
        {
            if (statusNames == null || !statusNames.Any())
            {
                return 0;
            }



            return await _context.TravelRequests
                .Include(tr => tr.CurrentStatus)
                .CountAsync(tr => tr.IsActive &&
                                   tr.CurrentStatus != null &&
                                   statusNames.Contains(tr.CurrentStatus.StatusName) &&
                                   (tr.UpdatedAt > tr.CreatedAt.Add(slaThreshold)));
        }
    }
}
