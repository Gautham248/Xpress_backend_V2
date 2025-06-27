namespace Xpress_backend_V2.Models
{


    public class User
    {
        public int UserId { get; set; } // PK
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<TravelRequest> TravelRequests { get; set; } = new List<TravelRequest>();
        public ICollection<TicketOption> CreatedTicketOptions { get; set; } = new List<TicketOption>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
        public ICollection<AadharDoc> AadharDocs { get; set; } = new List<AadharDoc>();
        public ICollection<PassportDoc> PassportDocs { get; set; } = new List<PassportDoc>();
        public ICollection<VisaDoc> VisaDocs { get; set; } = new List<VisaDoc>();
        public ICollection<AadharDoc> CreatedAadharDocs { get; set; } = new List<AadharDoc>();
        public ICollection<PassportDoc> CreatedPassportDocs { get; set; } = new List<PassportDoc>();
        public ICollection<VisaDoc> CreatedVisaDocs { get; set; } = new List<VisaDoc>();
        public ICollection<Notification> CreatedNotifications { get; set; } = new List<Notification>();
        public ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new List<WorkflowHistory>(); // Added to fix CS1061
    }

}
