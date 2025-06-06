namespace Xpress_backend_V2.Models
{
    public class VisaDoc
    {
        public int VisaDocId { get; set; } // PK
        public int UserId { get; set; } // FK → Users
        public string VisaNumber { get; set; }
        public string IssuingCountry { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string VisaClass { get; set; }
        public string DocumentPath { get; set; }
        public int CreatedBy { get; set; } // FK → Users
        public DateTime UploadedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User CreatedByUser { get; set; }
    }
}
