namespace Xpress_backend_V2.Models
{
    public class WorkflowRule
    {
        public int RuleId { get; set; }
        public int WorkflowId { get; set; }
        public string? UserRole { get; set; }
        public string? ProjectCode { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
        public RMT? RMT { get; set; }
    }

}
