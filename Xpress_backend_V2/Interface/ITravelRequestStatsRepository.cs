using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelRequestStatsRepository
    {
       
            Task<int> GetTodaysNewRequestsCountAsync();
            Task<int> GetTodaysRequestsByStatusNamesCountAsync(IEnumerable<string> statusNames);
            Task<int> GetRequestsByStatusNamesCountAsync(IEnumerable<string> statusNames);
            Task<TravelLegCountsDto> GetTodaysTravelLegCountsAsync();
            Task<int> GetSlaBreachedRequestsCountAsync(IEnumerable<string> statusNames, TimeSpan slaThreshold);
        
    }
}
