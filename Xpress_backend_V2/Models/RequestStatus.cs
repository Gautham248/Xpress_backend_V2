namespace Xpress_backend_V2.Models
{
    public class RequestStatus
    {
        public int StatusId { get; set; } // PK
        public string StatusName { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public int SequenceOrder { get; set; } = 0;
        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<TravelRequest> TravelRequests { get; set; } = new List<TravelRequest>();
        public ICollection<AuditLog> OldAuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<AuditLog> NewAuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>(); // Added to fix CS1061
    }
}
