namespace Xpress_backend_V2.Models
{
    public class PassportDoc
    {
        public int PassportDocId { get; set; } // PK
        public int UserId { get; set; } // FK → Users
        public string PassportNumber { get; set; }
        public string IssuingCountry { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string DocumentPath { get; set; }
        public int CreatedBy { get; set; } // FK → Users
        public DateTime UploadedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User CreatedByUser { get; set; }
    }
}
