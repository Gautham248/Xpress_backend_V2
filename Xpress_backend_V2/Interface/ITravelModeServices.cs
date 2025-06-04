using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelModeServices
    {
        Task<IEnumerable<TravelMode>> GetAllAsync();
        Task<TravelMode> GetByIdAsync(int travelModeId);
        Task AddAsync(TravelMode travelMode);
        Task UpdateAsync(TravelMode travelMode);
        Task DeleteAsync(int travelModeId);
    }
}
