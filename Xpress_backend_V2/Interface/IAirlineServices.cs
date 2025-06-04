using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAirlineServices
    {
        Task<IEnumerable<Airline>> GetAllAsync();
        Task<Airline> GetByIdAsync(int airlineId);
        Task AddAsync(Airline airline);
        Task UpdateAsync(Airline airline);
        Task DeleteAsync(int airlineId);
    }
}
