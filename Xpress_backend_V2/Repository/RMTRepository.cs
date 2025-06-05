using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class RMTRepository : IRMTServices
    {
        private readonly ApiDbContext _context;

        public RMTRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RMT>> GetAllAsync()
        {
            return await _context.RMTs
                .Include(r => r.TravelRequests)
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<RMT> GetByIdAsync(int projectId)
        {
            return await _context.RMTs
                .Include(r => r.TravelRequests)
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.IsActive);
        }

        public async Task<RMT> GetByProjectCodeAsync(string projectCode)
        {
            return await _context.RMTs
                .Include(r => r.TravelRequests)
                .FirstOrDefaultAsync(r => r.ProjectCode == projectCode && r.IsActive);
        }

        public async Task AddAsync(RMT rmt)
        {
            rmt.IsActive = true;
            _context.RMTs.Add(rmt);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RMT rmt)
        {
            _context.Entry(rmt).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int projectId)
        {
            var rmt = await _context.RMTs.FindAsync(projectId);
            if (rmt != null)
            {
                rmt.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetAllProjectCodesAsync()
        {
            return await _context.RMTs
                .Select(r => r.ProjectCode)
                .Distinct()
                .ToListAsync();
        }
    }
}
