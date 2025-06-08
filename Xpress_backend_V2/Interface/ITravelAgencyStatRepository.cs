using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelAgencyStatRepository
    {
        Task<IEnumerable<TravelAgencyStatDto>> GetTravelAgencyStatsAsync(DateTime startDate, DateTime endDate);
    }
}
