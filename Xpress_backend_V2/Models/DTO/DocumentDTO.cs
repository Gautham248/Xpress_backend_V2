using System.Globalization;

namespace Xpress_backend_V2.Models.DTO
{
    public class DocumentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string IDType { get; set; } // Passport, Visa, Aadhar
        public string DocumentPath { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Passport specific fields
        public string? PassportNumber { get; set; }
        public DateTime? PassportIssueDate { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public string? IssuingCountry { get; set; }

        // Visa specific fields
        public string? VisaNumber { get; set; }
        public DateTime? VisaIssueDate { get; set; }
        public DateTime? VisaExpiryDate { get; set; }
        public string? VisaClass { get; set; }

        // Aadhar specific fields
        public string? AadharName { get; set; }
        public string? AadharNumber { get; set; }
        public string? FullName { get; set; }
        public int CreatedBy { get; set; }
    }
}
