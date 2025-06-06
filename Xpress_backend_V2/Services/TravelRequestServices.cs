using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Repository;

namespace Xpress_backend_V2.Services
{
    public class TravelRequestService 
    {
        private readonly TravelRequestRepository _repository;
        private readonly ILogger<TravelRequestService> _logger;

        public TravelRequestService(TravelRequestRepository repository, ILogger<TravelRequestService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<TravelRequest>> GetTravelRequestsByUserIdAsync(int userId)
        {
            return await _repository.GetTravelRequestsByUserIdAsync(userId);
        }

        // Example method to map CurrentStatusId to a status name
        public async Task<string> GetStatusNameAsync(int statusId)
        {
            // This could query a Status table or use a static mapping
            var statusMap = new Dictionary<int, string>
        {
            { 1, "Pending" },
            { 2, "Approved" },
            { 3, "Tickets Selected" },
            { 4, "Tickets Dispatched" },
            // Add more mappings as needed
        };
            return statusMap.TryGetValue(statusId, out var statusName) ? statusName : "Unknown";
        }
    }
}
