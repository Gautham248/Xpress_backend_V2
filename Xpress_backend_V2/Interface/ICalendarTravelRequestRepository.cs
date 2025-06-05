using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface ICalendarTravelRequestRepository
    {
        Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsAsync();
        Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByRangeOptimizedAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CalendarTravelRequestDTO>> GetCalendarEventsByTypeAndRangeAsync(DateTime startDate, DateTime endDate, string eventType);
    }
}
