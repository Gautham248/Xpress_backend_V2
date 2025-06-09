using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IAuditLogHandlerService
    {
        Task ProcessAuditLogEntryAsync(AuditLog auditLog);
    }
}
