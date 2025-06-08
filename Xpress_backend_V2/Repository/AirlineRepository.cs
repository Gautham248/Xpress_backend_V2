using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class AirlineRepository : IAirlineServices
    {
        private readonly ApiDbContext _context;

        public AirlineRepository(ApiDbContext context)
        {
            _context = context;
        }

        // This now returns ALL individual flight bookings, not a distinct list of companies.
        public async Task<IEnumerable<Airline>> GetAllAsync()
        {
            // The old .Include(a => a.TravelRequests) is invalid.
            // We now include the single parent TravelRequest.
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .ToListAsync();
        }

        public async Task<Airline> GetByIdAsync(int airlineId)
        {
            // The old .Include(a => a.TravelRequests) is invalid.
            // We now include the single parent TravelRequest.
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .FirstOrDefaultAsync(a => a.AirlineId == airlineId);
        }

        // The calling service/controller is responsible for setting the RequestId
        // on the airline object before calling this method.
        public async Task AddAsync(Airline airline)
        {
            _context.Airlines.Add(airline);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Airline airline)
        {
            _context.Entry(airline).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int airlineId)
        {
            var airline = await _context.Airlines.FindAsync(airlineId);
            if (airline != null)
            {
                _context.Airlines.Remove(airline);
                await _context.SaveChangesAsync();
            }
        }

        // ----- IMPLEMENTATION OF NEW, MORE USEFUL METHODS -----

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetDistinctAirlineNamesAsync()
        {
            return await _context.Airlines
                .Select(a => a.AirlineName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Airline>> GetAirlinesByRequestIdAsync(string requestId)
        {
            return await _context.Airlines
                .Where(a => a.RequestId == requestId)
                .ToListAsync();
        }
    }
}