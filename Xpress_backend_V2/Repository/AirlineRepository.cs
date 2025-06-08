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

       
        public async Task<IEnumerable<Airline>> GetAllAsync()
        {
  
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .ToListAsync();
        }

        public async Task<Airline> GetByIdAsync(int airlineId)
        {
            
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .FirstOrDefaultAsync(a => a.AirlineId == airlineId);
        }

       
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

        public async Task<IEnumerable<string>> GetDistinctAirlineNamesAsync()
        {
            return await _context.Airlines
                .Select(a => a.AirlineName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Airline>> GetAirlinesByRequestIdAsync(string requestId)
        {
            return await _context.Airlines
                .Where(a => a.RequestId == requestId)
                .ToListAsync();
        }
    }
}