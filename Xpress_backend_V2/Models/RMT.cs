namespace Xpress_backend_V2.Models
{
    public class RMT
    {
        public int ProjectId { get; set; } // PK
        public string ProjectCode { get; set; } = string.Empty; // Unique, FK → TravelRequests
        public string ProjectName { get; set; } = string.Empty;
        public int DuId { get; set; }
        public DateTime ProjectStartDate { get; set; }
        public DateTime ProjectEndDate { get; set; }
        public string ProjectManager { get; set; } = string.Empty;
        public string ProjectManagerEmail { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = string.Empty;
        public string DuHeadName { get; set; } = string.Empty;
        public string DuHeadEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Navigation property
        public ICollection<TravelRequest> TravelRequests { get; set; } = new List<TravelRequest>();
        public ICollection<WorkflowRule> WorkflowRules { get; set; } = new List<WorkflowRule>(); // Added to fix CS1061
    }
}
