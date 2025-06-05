namespace Xpress_backend_V2.Models.DTO
{
    public class AuditLogResponseDTO
    {
        public int LogId { get; set; }
        public string RequestId { get; set; }
        public int UserId { get; set; }
        public string ActionType { get; set; }
        public int? OldStatusId { get; set; }
        public int? NewStatusId { get; set; }
        public string ChangeDescription { get; set; }  // Generated server-side
        public string Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
