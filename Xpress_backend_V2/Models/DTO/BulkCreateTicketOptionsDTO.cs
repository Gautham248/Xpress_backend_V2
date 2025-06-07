using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class BulkCreateTicketOptionsDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one ticket option is required.")]
        public List<CreateTicketOptionDTO> TicketOptions { get; set; } = new List<CreateTicketOptionDTO>();

        public string? Comments { get; set; }
    }
}
