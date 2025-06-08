using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class AirlineReportRepository : IAirlineReportRepository
    {
        private readonly ApiDbContext _context;

        public AirlineReportRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AirlineReportDto>> GetAirlineReportAsync(DateTime startDate, DateTime endDate)
        {
            // Ensure the end date includes the entire day
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var report = await _context.TravelRequests
                // Filter requests that have an assigned airline
                .Where(tr => tr.AirlineId != null)
                // Filter by the date range using OutboundDepartureDate
                .Where(tr => tr.OutboundDepartureDate >= startDate.Date && tr.OutboundDepartureDate <= inclusiveEndDate)
                // Group by both Airline Name and the IsInternational flag
                .GroupBy(tr => new
                {
                    tr.Airline.AirlineName,
                    tr.IsInternational
                })
                // Project the grouped data into our DTO
                .Select(g => new AirlineReportDto
                {
                    AirlineName = g.Key.AirlineName,
                    TypeOfTravel = g.Key.IsInternational ? "International" : "Domestic",
                    TravelRequestCount = g.Count(),
                    // Sum the TotalExpense for each group. Handle potential nulls with '?? 0'
                    TotalAirlineExpense = g.Sum(tr => tr.TotalExpense ?? 0)
                })
                .OrderBy(dto => dto.AirlineName)
                .ThenBy(dto => dto.TypeOfTravel)
                .ToListAsync();

            return report;
        }
    }
}
