namespace Xpress_backend_V2.Models
{
    public class AadharDoc
    {
        public int AadharId { get; set; } // PK
        public int UserId { get; set; } // FK → Users
        public string AadharName { get; set; }
        public string? AadharNumber { get; set; }
        public string DocumentPath { get; set; }
        public int CreatedBy { get; set; } // FK → Users
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User CreatedByUser { get; set; }
    }
}
