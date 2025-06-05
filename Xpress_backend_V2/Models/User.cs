namespace Xpress_backend_V2.Models
{
    public class User
    {
        public int UserId { get; set; } // PK
        public string EmployeeName { get; set; }
        public string EmployeeEmail { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; } 
        public string UserRole { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<TravelRequest> TravelRequests { get; set; }
        public ICollection<TicketOption> CreatedTicketOptions { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
        public ICollection<UserNotification> UserNotifications { get; set; }
        public ICollection<AadharDoc> AadharDocs { get; set; }
        public ICollection<PassportDoc> PassportDocs { get; set; }
        public ICollection<VisaDoc> VisaDocs { get; set; }
        public ICollection<AadharDoc> CreatedAadharDocs { get; set; }
        public ICollection<PassportDoc> CreatedPassportDocs { get; set; }
        public ICollection<VisaDoc> CreatedVisaDocs { get; set; }
        public ICollection<Notification> CreatedNotifications { get; set; }
    }
}
