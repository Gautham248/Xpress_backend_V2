namespace Xpress_backend_V2.Models.DTO
{
    public class AuditLogDTO
    {
        public string RequestId { get; set; }
        public int UserId { get; set; }
        public string ActionType { get; set; }
        public int? OldStatusId { get; set; }
        public int? NewStatusId { get; set; }
        public string? CustomChangeDescription { get; set; }
        public string? Comments { get; set; }
    }
}
