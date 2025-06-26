namespace Xpress_backend_V2.Models
{
    public class WorkflowHistory
    {
        public int HistoryId { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public int? OldWorkflowId { get; set; }
        public int NewWorkflowId { get; set; }
        public int ChangedBy { get; set; }
        public string? ChangeReason { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public TravelRequest TravelRequest { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
        public WorkflowTemplate? OldWorkflow { get; set; }
        public WorkflowTemplate NewWorkflow { get; set; } = null!;
    }
}
