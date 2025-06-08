using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class TravelAgencyStatRepository : ITravelAgencyStatRepository
    {
        private readonly ApiDbContext _context; 

        public TravelAgencyStatRepository(ApiDbContext context) // Inject your DbContext
        {
            _context = context;
        }

        public async Task<IEnumerable<TravelAgencyStatDto>> GetTravelAgencyStatsAsync(DateTime startDate, DateTime endDate)
        {
            
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var stats = await _context.TravelRequests
                // 1. Filter out requests that don't have an agency name or an expense.
                .Where(tr => !string.IsNullOrEmpty(tr.TravelAgencyName) && tr.TravelAgencyExpense.HasValue)

                // 2. Filter by the date range based on the outbound departure date.
                .Where(tr => tr.OutboundDepartureDate >= startDate.Date && tr.OutboundDepartureDate <= adjustedEndDate)

                // 3. Group by both the agency name AND the type of travel (IsInternational).
                .GroupBy(tr => new { tr.TravelAgencyName, tr.IsInternational })

                // 4. Project the grouped data into our DTO.
                .Select(group => new TravelAgencyStatDto
                {
                    TravelAgencyName = group.Key.TravelAgencyName,
                    TravelType = group.Key.IsInternational ? "International" : "Domestic",
                    RequestCount = group.Count(),
              
                    TotalExpense = group.Sum(tr => tr.TravelAgencyExpense.Value)
                })
                .OrderBy(s => s.TravelAgencyName).ThenBy(s => s.TravelType) 
                .ToListAsync();

            return stats;
        }
    }
}
