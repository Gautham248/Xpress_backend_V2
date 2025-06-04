namespace Xpress_backend_V2.Models
{
    public class RequestStatus
    {
        public int StatusId { get; set; } // PK
        public string StatusName { get; set; }
        public string StatusDescription { get; set; }
        public int SequenceOrder { get; set; }       
        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<TravelRequest> TravelRequests { get; set; }
        public ICollection<AuditLog> OldAuditLogs { get; set; }
        public ICollection<AuditLog> NewAuditLogs { get; set; }
    }
}
