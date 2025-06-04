using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelRequestServices
    {
        Task<TravelRequest> CreateTravelRequestAsync(TravelRequest travelRequest);
        Task<IEnumerable<TravelRequest>> GetAllAsync();
        Task<TravelRequest> GetByIdAsync(string requestId);
        Task AddAsync(TravelRequest travelRequest);
        Task UpdateAsync(TravelRequest travelRequest);
        Task DeleteAsync(string requestId);
        Task<IEnumerable<TravelRequest>> GetByStatusAsync(int statusId);
        Task<IEnumerable<TravelRequest>> GetByUserAsync(int userId);
    }
}
