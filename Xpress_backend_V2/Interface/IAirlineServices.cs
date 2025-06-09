using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAirlineServices
    {
        
        Task<IEnumerable<Airline>> GetAllAsync();

        /// <summary>
        /// Gets a single flight booking by its unique ID.
        /// </summary>
        Task<Airline> GetByIdAsync(int airlineId);

        Task AddAsync(Airline airline);

        /// <summary>
        /// Updates an existing flight booking.
        /// </summary>
        Task UpdateAsync(Airline airline);

        /// <summary>
        /// Deletes a flight booking by its ID.
        /// </summary>
        Task DeleteAsync(int airlineId);

        Task<IEnumerable<string>> GetDistinctAirlineNamesAsync();

        /// <summary>
        /// Gets all flight bookings associated with a specific Travel Request.
        /// </summary>
        Task<IEnumerable<Airline>> GetAirlinesByRequestIdAsync(string requestId);
    
}
}
