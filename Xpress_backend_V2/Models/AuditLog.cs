namespace Xpress_backend_V2.Models
{
    public class AuditLog
    {
        public int LogId { get; set; } // PK
        public string RequestId { get; set; } // FK → TravelRequests
        public int UserId { get; set; } // FK → Users
        public string ActionType { get; set; }
        public DateTime ActionDate { get; set; }
        public int? OldStatusId { get; set; } // Nullable FK → RequestStatuses
        public int? NewStatusId { get; set; } // Nullable FK → RequestStatuses
        public string? ChangeDescription { get; set; }
        public string? Comments { get; set; }
        public DateTime Timestamp { get; set; }

        // Navigation properties
        public TravelRequest TravelRequest { get; set; }
        public User User { get; set; }
        public RequestStatus? OldStatus { get; set; }
        public RequestStatus? NewStatus { get; set; }
    }
}
