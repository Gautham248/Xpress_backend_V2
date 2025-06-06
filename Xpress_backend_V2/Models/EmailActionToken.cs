using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models
{
    public class EmailActionToken
    {
        [Key]
        public int TokenId { get; set; }
        [Required]
        public string TokenValue { get; set; } // Securely Generated Token (e.g., GUID)
        [Required]
        public string RequestId { get; set; } // FK -> TravelRequests(RequestId)
        public virtual TravelRequest TravelRequest { get; set; }
        [Required]
        public string UserEmail { get; set; } // Email of the user this token is for (Manager, DU Head)
        [Required]
        public string ActionType { get; set; } // e.g., "ManagerApprove", "ManagerReject", "DuHeadApprove", "SelectTicketOption"
        public int? OptionId { get; set; } // Nullable, FK -> TicketOptions(OptionId) if action is "SelectTicketOption"
        public virtual TicketOption TicketOption { get; set; } // Navigation for OptionId
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
