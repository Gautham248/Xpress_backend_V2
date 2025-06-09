namespace Xpress_backend_V2.Models.DTO
{
    public class EditedRequestAuditLogDto
    {
        public int Id { get; set; }
        public string? ActionType { get; set; }
        public DateTime ActionDate { get; set; }

        // FIX: Change from 'int' to 'int?' to match the AuditLog entity
        public int? OldStatusId { get; set; }
        public int? NewStatusId { get; set; }

        public string? ChangeDescription { get; set; }
    }
}
