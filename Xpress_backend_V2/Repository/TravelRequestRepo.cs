using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class TravelRequestRepo : ITravelRequestRepo
    {
        private readonly ApiDbContext _context;

        public TravelRequestRepo(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<TravelRequest> GetByIdAsync(string requestId)
        {
            return await _context.TravelRequests
                                 .FirstOrDefaultAsync(r => r.RequestId == requestId);
        }

        public void Update(TravelRequest travelRequest)
        {
            _context.TravelRequests.Update(travelRequest);
        }

        public void AddAuditLog(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
