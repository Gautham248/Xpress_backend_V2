using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAuditLogServices
    {

        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<AuditLog> GetByIdAsync(int logId);
        Task AddAsync(AuditLog auditLog);
        Task UpdateAsync(AuditLog auditLog);
        Task DeleteAsync(int logId);
        Task<IEnumerable<AuditLog>> GetByTravelRequestAsync(string requestId);

        Task<AuditLog> CreateAuditLogAsync(AuditLog auditLog);
    }
}
