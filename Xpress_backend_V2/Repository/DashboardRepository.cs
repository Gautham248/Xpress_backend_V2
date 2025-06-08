using static Xpress_backend_V2.Models.DTO.DashboardDtos;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Data;
using Microsoft.EntityFrameworkCore;

namespace Xpress_backend_V2.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApiDbContext _context;

        public DashboardRepository(ApiDbContext context)
        {
            _context = context;
        }

        // API 1 Implementation
        public async Task<RequestStatusOverviewDto> GetRequestStatusOverviewAsync(DateTime startDate, DateTime endDate)
        {
            // Set up the universal date filter. This is reusable and efficient.
            var baseQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt.Date >= startDate.Date && tr.CreatedAt.Date <= endDate.Date);

           
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
                .ToListAsync();

            // 3. Assemble the final DTO
            return new RequestStatusOverviewDto
            {
                Requests = requestsList,
                TotalRequestCount = totalCount,
                RejectedCount = rejectedCount,
                ConfirmedOrOtherCount = confirmedOrOtherCount
            };
        }

        // API 2 Implementation
        public async Task<ExpenseOverviewDto> GetExpenseOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var baseQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt.Date >= startDate.Date && tr.CreatedAt.Date <= endDate.Date);

            // 1. Calculate the expense summaries
            var totalExpense = await baseQuery.SumAsync(tr => tr.TotalExpense ?? 0);
            var domesticExpense = await baseQuery.Where(tr => !tr.IsInternational).SumAsync(tr => tr.TotalExpense ?? 0);
            var internationalExpense = await baseQuery.Where(tr => tr.IsInternational).SumAsync(tr => tr.TotalExpense ?? 0);

            // 2. Fetch the detailed list
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
                .ToListAsync();

            // 3. Assemble the final DTO
            return new ExpenseOverviewDto
            {
                Requests = requestsList,
                TotalExpense = totalExpense,
                DomesticExpense = domesticExpense,
                InternationalExpense = internationalExpense
            };
        }

        // API 3 Implementation
        public async Task<TripDetailsOverviewDto> GetTripDetailsOverviewAsync(DateTime startDate, DateTime endDate)
        {
           
            var validTripStatuses = new[] { "Confirmed", "InTransit", "Returned", "Closed" };

           
            var filteredQuery = _context.TravelRequests
                .Where(tr => tr.CreatedAt.Date >= startDate.Date && tr.CreatedAt.Date <= endDate.Date)
                .Where(tr => validTripStatuses.Contains(tr.CurrentStatus.StatusName));

          
            var totalTripCount = await filteredQuery.CountAsync();
            var domesticTripCount = await filteredQuery.CountAsync(tr => !tr.IsInternational);
            var internationalTripCount = await filteredQuery.CountAsync(tr => tr.IsInternational);

            // 2. Fetch the detailed list from the same filtered query
            var tripsList = await filteredQuery
                .Include(tr => tr.CurrentStatus)
                .Include(tr => tr.Airline) 
                .Select(tr => new TripDetailItemDto
                {
                    ID = tr.RequestId,
                    RequestDate = tr.CreatedAt,
                    Status = tr.CurrentStatus.StatusName,
                    TravelType = tr.IsInternational ? "International" : "Domestic",
                    Airline = tr.Airline != null ? tr.Airline.AirlineName : "N/A", // Safely access related data
                    TravelAgency = tr.TravelAgencyName ?? "N/A"
                })
                .ToListAsync();

            // 3. Assemble the final DTO
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
