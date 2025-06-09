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
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

           
            var report = await _context.Airlines
                // 1. Filter for airlines that are actually linked to a travel request
                //    and whose request falls within the specified date range.
                .Where(airline => airline.RequestId != null &&
                                  airline.TravelRequest.OutboundDepartureDate >= startDate.Date &&
                                  airline.TravelRequest.OutboundDepartureDate <= inclusiveEndDate)
                // 2. Group by the Airline's Name and the Travel Type from its parent request.
                .GroupBy(airline => new
                {
                    airline.AirlineName,
                    airline.TravelRequest.IsInternational
                })
                // 3. Project the grouped data into our DTO.
                .Select(group => new AirlineReportDto
                {
                    AirlineName = group.Key.AirlineName,
                    TypeOfTravel = group.Key.IsInternational ? "International" : "Domestic",
                    // Count the number of *distinct* Travel Requests this airline was a part of.
                    // This prevents double-counting if an airline is used multiple times on one request.
                    TravelRequestCount = group.Select(a => a.RequestId).Distinct().Count(),
                    // Sum the specific expense for each airline booking within the group.
                    TotalAirlineExpense = group.Sum(a => (decimal)a.AirlineExpense)
                })
                .OrderBy(dto => dto.AirlineName)
                .ThenBy(dto => dto.TypeOfTravel)
                .ToListAsync();

            return report;
        }
    }
}