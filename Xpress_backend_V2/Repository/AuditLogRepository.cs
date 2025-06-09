using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Repository
{
    public class AuditLogRepository : IAuditLogServices
    {
        private readonly ApiDbContext _context;

        public AuditLogRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _context.AuditLogs
                .Include(al => al.TravelRequest)
                .Include(al => al.User)
                .Include(al => al.OldStatus)
                .Include(al => al.NewStatus)
                .ToListAsync();
        }

        public async Task<AuditLog> GetByIdAsync(int logId)
        {
            return await _context.AuditLogs
                .Include(al => al.TravelRequest)
                .Include(al => al.User)
                .Include(al => al.OldStatus)
                .Include(al => al.NewStatus)
                .FirstOrDefaultAsync(al => al.LogId == logId);
        }

        public async Task AddAsync(AuditLog auditLog)
        {
            auditLog.ActionDate = DateTime.UtcNow;
            auditLog.Timestamp = DateTime.UtcNow;
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AuditLog auditLog)
        {
            _context.Entry(auditLog).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int logId)
        {
            var auditLog = await _context.AuditLogs.FindAsync(logId);
            if (auditLog != null)
            {
                _context.AuditLogs.Remove(auditLog);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<AuditLog>> GetByTravelRequestAsync(string requestId)
        {
            return await _context.AuditLogs
                .Include(al => al.TravelRequest)
                .Include(al => al.User)
                .Include(al => al.OldStatus)
                .Include(al => al.NewStatus)
                .Where(al => EF.Functions.ILike(al.RequestId, requestId))
                .ToListAsync();
        }

        public async Task<AuditLog> CreateAuditLogAsync(AuditLog auditLog)
        {
            auditLog.Timestamp = DateTime.UtcNow;
            auditLog.Comments ??= string.Empty;
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }


    }
}