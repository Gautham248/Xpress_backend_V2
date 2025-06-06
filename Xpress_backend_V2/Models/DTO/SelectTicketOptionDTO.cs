using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class SelectTicketOptionDTO
    {
        [Required]
        public int SelectingUserId { get; set; }
        public string? Comments { get; set; }
    }
}
