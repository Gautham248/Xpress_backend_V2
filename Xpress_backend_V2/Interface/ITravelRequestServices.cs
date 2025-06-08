using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelRequestServices
    {
        Task<IEnumerable<TravelRequest>> GetAllAsync();
        Task<TravelRequest> GetByIdAsync(string requestId);
        Task AddAsync(TravelRequest travelRequest);
        Task UpdateAsync(TravelRequest travelRequest);
        Task DeleteAsync(string requestId);
        Task<IEnumerable<TravelRequest>> GetByStatusAsync(int statusId);
        Task<IEnumerable<TravelRequest>> GetByUserAsync(int userId);

        // Travel Request
        //Task<TravelRequest> GetAllTravelRequestsAsync();

        // Travel Info Banner
        Task<List<TravelInfoBannerDTO>> GetTravelInfoBannerDetailsAsync(string requestId);

        Task<TravelRequest> CreateTravelRequestAsync(TravelRequest travelRequest);

        // Travel Info
        Task<List<TravelInfoDTO>> GetTravelInfoAsync(string requestId);
        Task<TravelRequest> GetTravelRequestByIdAsync(string requestId);

        Task<TravelRequest> UpdateTravelRequestAsync(TravelRequest travelRequestEntity);

        Task<TravelRequestTimelineDTO?> GetTimelineAsync(string requestId);

        Task<IEnumerable<UserTravelRequestDTO>> GetTravelRequestsByUserIdAsync(int userId);
    }
}
