using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAirlineServices
    {
        /// <summary>
        /// Gets all individual flight bookings from the database.
        /// WARNING: This does NOT return a distinct list of airline companies.
        /// </summary>
        Task<IEnumerable<Airline>> GetAllAsync();

        /// <summary>
        /// Gets a single flight booking by its unique ID.
        /// </summary>
        Task<Airline> GetByIdAsync(int airlineId);

        /// <summary>
        /// Adds a new flight booking to the database. The RequestId must be set
        /// on the airline object before calling this method.
        /// </summary>
        Task AddAsync(Airline airline);

        /// <summary>
        /// Updates an existing flight booking.
        /// </summary>
        Task UpdateAsync(Airline airline);

        /// <summary>
        /// Deletes a flight booking by its ID.
        /// </summary>
        Task DeleteAsync(int airlineId);

        // ----- NEW METHODS -----

        /// <summary>
        /// Gets a distinct, sorted list of airline company names.
        /// Ideal for populating UI dropdowns.
        /// </summary>
        Task<IEnumerable<string>> GetDistinctAirlineNamesAsync();

        /// <summary>
        /// Gets all flight bookings associated with a specific Travel Request.
        /// </summary>
        Task<IEnumerable<Airline>> GetAirlinesByRequestIdAsync(string requestId);
    
}
}
