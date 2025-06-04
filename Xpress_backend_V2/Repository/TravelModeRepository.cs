using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class TravelModeRepository : ITravelModeServices
    {
        private readonly ApiDbContext _context;

        public TravelModeRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TravelMode>> GetAllAsync()
        {
            return await _context.TravelModes
                .Include(tm => tm.TravelRequests)
                .ToListAsync();
        }

        public async Task<TravelMode> GetByIdAsync(int travelModeId)
        {
            return await _context.TravelModes
                .Include(tm => tm.TravelRequests)
                .FirstOrDefaultAsync(tm => tm.TravelModeId == travelModeId);
        }

        public async Task AddAsync(TravelMode travelMode)
        {
            _context.TravelModes.Add(travelMode);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TravelMode travelMode)
        {
            _context.Entry(travelMode).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int travelModeId)
        {
            var travelMode = await _context.TravelModes.FindAsync(travelModeId);
            if (travelMode != null)
            {
                _context.TravelModes.Remove(travelMode);
                await _context.SaveChangesAsync();
            }
        }
    }
}
