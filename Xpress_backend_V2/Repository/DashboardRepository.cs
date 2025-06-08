using static Xpress_backend_V2.Models.DTO.DashboardDtos;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Data;
using Microsoft.EntityFrameworkCore;

namespace Xpress_backend_V2.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<DashboardRepository> _logger;

        public DashboardRepository(ApiDbContext context, ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // API 1 Implementation - GetRequestStatusOverviewAsync
        public async Task<RequestStatusOverviewDto> GetRequestStatusOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1); // Ensure end date is inclusive
            _logger.LogInformation("Fetching request status overview for period {StartDate} to {EndDate}", startDate, inclusiveEndDate);

            var baseQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt >= startDate.Date && tr.CreatedAt <= inclusiveEndDate);

            var totalCount = await baseQuery.CountAsync();
            var rejectedCount = await baseQuery.CountAsync(tr => tr.CurrentStatus.StatusName == "Rejected");

            var confirmedOrOtherStatuses = new[] { "Confirmed", "InTransit", "Returned", "Closed" };
            var confirmedOrOtherCount = await baseQuery.CountAsync(tr => confirmedOrOtherStatuses.Contains(tr.CurrentStatus.StatusName));

            var requestsList = await baseQuery
                .Include(tr => tr.CurrentStatus)
                .Select(tr => new RequestStatusItemDto
                {
                    ID = tr.RequestId,
                    RequestDate = tr.CreatedAt,
                    Status = tr.CurrentStatus.StatusName,
                    TravelType = tr.IsInternational ? "International" : "Domestic"
                })
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return new RequestStatusOverviewDto
            {
                Requests = requestsList,
                TotalRequestCount = totalCount,
                RejectedCount = rejectedCount,
                ConfirmedOrOtherCount = confirmedOrOtherCount
            };
        }

        // API 2 Implementation - GetExpenseOverviewAsync
        public async Task<ExpenseOverviewDto> GetExpenseOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            _logger.LogInformation("Fetching expense overview for period {StartDate} to {EndDate}", startDate, inclusiveEndDate);

            var baseQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt >= startDate.Date && tr.CreatedAt <= inclusiveEndDate);

            var totalExpense = await baseQuery.SumAsync(tr => tr.TotalExpense ?? 0);
            var domesticExpense = await baseQuery.Where(tr => !tr.IsInternational).SumAsync(tr => tr.TotalExpense ?? 0);
            var internationalExpense = await baseQuery.Where(tr => tr.IsInternational).SumAsync(tr => tr.TotalExpense ?? 0);

            var requestsList = await baseQuery
                .Include(tr => tr.CurrentStatus)
                .Select(tr => new RequestExpenseItemDto
                {
                    ID = tr.RequestId,
                    RequestDate = tr.CreatedAt,
                    Status = tr.CurrentStatus.StatusName,
                    TravelType = tr.IsInternational ? "International" : "Domestic",
                    EstimatedCost = tr.TotalExpense
                })
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return new ExpenseOverviewDto
            {
                Requests = requestsList,
                TotalExpense = totalExpense,
                DomesticExpense = domesticExpense,
                InternationalExpense = internationalExpense
            };
        }

        // API 3 Implementation - GetTripDetailsOverviewAsync
        public async Task<TripDetailsOverviewDto> GetTripDetailsOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            _logger.LogInformation("Fetching trip details overview for period {StartDate} to {EndDate}", startDate, inclusiveEndDate);

            var validTripStatuses = new[] { "Confirmed", "InTransit", "Returned", "Closed" };

            var filteredQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt >= startDate.Date && tr.CreatedAt <= inclusiveEndDate)
                .Where(tr => validTripStatuses.Contains(tr.CurrentStatus.StatusName));

            var totalTripCount = await filteredQuery.CountAsync();
            var domesticTripCount = await filteredQuery.CountAsync(tr => !tr.IsInternational);
            var internationalTripCount = await filteredQuery.CountAsync(tr => tr.IsInternational);

            var tripsList = await filteredQuery
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.BookedAirlines)
                .Select(tr => new TripDetailItemDto
                {
                    ID = tr.RequestId,
                    RequestDate = tr.CreatedAt,
                    Status = tr.CurrentStatus.StatusName,
                    TravelType = tr.IsInternational ? "International" : "Domestic",
                    Airline = tr.BookedAirlines.Any() ? tr.BookedAirlines.FirstOrDefault().AirlineName : "N/A",
                    TravelAgency = tr.TravelAgencyName ?? "N/A"
                })
                .OrderByDescending(t => t.RequestDate)
                .ToListAsync();

            return new TripDetailsOverviewDto
            {
                Trips = tripsList,
                TotalTripCount = totalTripCount,
                DomesticTripCount = domesticTripCount,
                InternationalTripCount = internationalTripCount
            };
        }
    }
}