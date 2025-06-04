using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class RequestStatusRepository : IRequestStatusServices
    {
        private readonly ApiDbContext _context;

        public RequestStatusRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RequestStatus>> GetAllAsync()
        {
            return await _context.RequestStatuses
                .Include(rs => rs.TravelRequests)
                .Where(rs => rs.IsActive)
                .ToListAsync();
        }

        public async Task<RequestStatus> GetByIdAsync(int statusId)
        {
            return await _context.RequestStatuses
                .Include(rs => rs.TravelRequests)
                .FirstOrDefaultAsync(rs => rs.StatusId == statusId && rs.IsActive);
        }

        public async Task AddAsync(RequestStatus requestStatus)
        {
            requestStatus.IsActive = true;
            _context.RequestStatuses.Add(requestStatus);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RequestStatus requestStatus)
        {
            _context.Entry(requestStatus).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int statusId)
        {
            var requestStatus = await _context.RequestStatuses.FindAsync(statusId);
            if (requestStatus != null)
            {
                requestStatus.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
