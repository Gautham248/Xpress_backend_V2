using static Xpress_backend_V2.Models.DTO.DashboardDtos;

namespace Xpress_backend_V2.Interface
{
    public interface IDashboardRepository
    {
        
        /// Gets an overview of travel requests with status counts based on a date range.
        
        
        /// <returns>A DTO containing a list of requests and relevant status counts.</returns>
        Task<RequestStatusOverviewDto> GetRequestStatusOverviewAsync(DateTime startDate, DateTime endDate);

     
        /// Gets an overview of travel expenses (total, domestic, international) based on a date range.
     
   
        /// <returns>A DTO containing a list of requests and expense summaries.</returns>
        Task<ExpenseOverviewDto> GetExpenseOverviewAsync(DateTime startDate, DateTime endDate);

       
        /// Gets an overview of trips with specific statuses (Confirmed, InTransit, Returned, Closed)
        /// and counts for total, domestic, and international trips based on a date range.
      
        /// <returns>A DTO containing a list of trips and relevant trip counts.</returns>
        Task<TripDetailsOverviewDto> GetTripDetailsOverviewAsync(DateTime startDate, DateTime endDate);
    
}
}
