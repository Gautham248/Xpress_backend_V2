namespace Xpress_backend_V2.Models
{
    public class WorkflowStep
    {
        public int StepId { get; set; }
        public int WorkflowId { get; set; }
        public int StatusId { get; set; }
        public int StepOrder { get; set; }
        public bool RequiresApproval { get; set; } = false;
        public string? ApproverRole { get; set; }
        public string? StepDescription { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
        public RequestStatus RequestStatus { get; set; } = null!;
    }
}
