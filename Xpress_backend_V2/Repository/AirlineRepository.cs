using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class AirlineRepository : IAirlineServices
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<AirlineRepository> _logger;

        public AirlineRepository(ApiDbContext context, ILogger<AirlineRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Airline>> GetAllAsync()
        {
            _logger.LogInformation("Getting all airline segments with their travel requests.");
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .ToListAsync();
        }

        public async Task<Airline> GetByIdAsync(int airlineId)
        {
            _logger.LogInformation("Getting airline segment by ID: {AirlineId} with its travel request.", airlineId);
            return await _context.Airlines
                .Include(a => a.TravelRequest)
                .FirstOrDefaultAsync(a => a.AirlineId == airlineId);
        }

        public async Task AddAsync(Airline airline)
        {
            if (string.IsNullOrEmpty(airline.RequestId))
            {
                _logger.LogWarning("Attempting to add an airline segment without a RequestId. AirlineName: {AirlineName}", airline.AirlineName);
            }
            _logger.LogInformation("Adding new airline segment: {AirlineName} for RequestId: {RequestId}", airline.AirlineName, airline.RequestId);
            _context.Airlines.Add(airline);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Airline airline)
        {
            _logger.LogInformation("Updating airline segment with ID: {AirlineId}", airline.AirlineId);
            var existingAirline = await _context.Airlines.FindAsync(airline.AirlineId);
            if (existingAirline != null)
            {
                _context.Entry(existingAirline).CurrentValues.SetValues(airline);
                existingAirline.RequestId = _context.Entry(existingAirline).Property(x => x.RequestId).OriginalValue;

                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Airline segment with ID: {AirlineId} not found for update.", airline.AirlineId);
            }
        }

        public async Task DeleteAsync(int airlineId)
        {
            _logger.LogInformation("Attempting to delete airline segment with ID: {AirlineId}", airlineId);
            var airline = await _context.Airlines.FindAsync(airlineId);
            if (airline != null)
            {
                _context.Airlines.Remove(airline);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted airline segment with ID: {AirlineId}", airlineId);
            }
            else
            {
                _logger.LogWarning("Airline segment with ID: {AirlineId} not found for deletion.", airlineId);
            }
        }
    }
}