namespace Xpress_backend_V2.Models
{
    public class RMT
    {
        public int ProjectId { get; set; } // PK
        public string ProjectCode { get; set; } // Unique, FK → TravelRequests
        public string ProjectName { get; set; }
        public int DuId { get; set; }
        public DateTime ProjectStartDate { get; set; }
        public DateTime ProjectEndDate { get; set; }
        public string ProjectManager { get; set; }
        public string ProjectManagerEmail { get; set; }
        public string ProjectStatus { get; set; }
        public string DuHeadName { get; set; }
        public string DuHeadEmail { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public ICollection<TravelRequest> TravelRequests { get; set; }
    }
}
