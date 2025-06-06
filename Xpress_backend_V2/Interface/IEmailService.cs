using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface IEmailService
    {
        Task ProcessAuditLogAsync(AuditLog auditLog);
        Task<string> GenerateEmailTokenAsync(string requestId, string action, string userEmail);
        Task<bool> ValidateEmailTokenAsync(string token, string requestId, string action);
    }
}
