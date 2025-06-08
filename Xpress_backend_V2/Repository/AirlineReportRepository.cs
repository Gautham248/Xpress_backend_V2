using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO; // Assuming AirlineReportDto is here
using System.Linq; // Required for LINQ methods like GroupBy, Select, Sum, etc.

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
            // Ensure the end date includes the entire day for accurate range filtering
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var report = await _context.Airlines // <<<<----- START FROM AIRLINES (SEGMENTS)
                .Include(a => a.TravelRequest) // Include the parent TravelRequest to access its properties
                .Where(a => a.TravelRequest != null && // Ensure the segment is linked to a TravelRequest
                             a.TravelRequest.IsActive && // Optionally filter by active travel requests
                             a.TravelRequest.OutboundDepartureDate >= startDate.Date &&
                             a.TravelRequest.OutboundDepartureDate <= inclusiveEndDate)
                .GroupBy(a => new // Group by AirlineName and the TravelRequest's IsInternational flag
                {
                    a.AirlineName, // This is the name of the airline for the segment
                    a.TravelRequest.IsInternational
                })
                .Select(g => new AirlineReportDto
                {
                    AirlineName = g.Key.AirlineName,
                    TypeOfTravel = g.Key.IsInternational ? "International" : "Domestic",
                    // Count how many distinct travel requests this airline was part of for this type of travel
                    // Or, if each Airline row is a segment and you want to count segments: g.Count()
                    TravelRequestCount = g.Select(x => x.RequestId).Distinct().Count(),
                    // Sum the AirlineExpense for each segment in the group
                    TotalAirlineExpense = g.Sum(x => (decimal)x.AirlineExpense) // Cast double to decimal
                })
                .OrderBy(dto => dto.AirlineName)
                .ThenBy(dto => dto.TypeOfTravel)
                .ToListAsync();

            return report;
        }
    }
}