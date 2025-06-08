namespace Xpress_backend_V2.Models.DTO
{
    public class PassportStatusDto
    {
        public string EmployeeName { get; set; }
        public string EmployeeEmail { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Department { get; set; } 
        public string DocStatus { get; set; } // "Expired" or "Not Expired"
    }
}
