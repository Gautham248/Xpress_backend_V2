using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Interface
{
    public interface ITravelRequestRepo
    {
        Task<TravelRequest> GetByIdAsync(string requestId);
        void Update(TravelRequest travelRequest);
        void AddAuditLog(AuditLog auditLog);
        Task<bool> SaveChangesAsync();
    }
}
