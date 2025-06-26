namespace Xpress_backend_V2.Models
{
    public class WorkflowTemplate
    {
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public string? WorkflowDescription { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
        public ICollection<WorkflowRule> WorkflowRules { get; set; } = new List<WorkflowRule>();
        public ICollection<WorkflowHistory> WorkflowHistoriesAsOld { get; set; } = new List<WorkflowHistory>();
        public ICollection<WorkflowHistory> WorkflowHistoriesAsNew { get; set; } = new List<WorkflowHistory>();
        public ICollection<TravelRequest> TravelRequests { get; set; } = new List<TravelRequest>();
    }
}
