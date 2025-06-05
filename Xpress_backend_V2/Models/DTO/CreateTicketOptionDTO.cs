using System.ComponentModel.DataAnnotations;

namespace Xpress_backend_V2.Models.DTO
{
    public class CreateTicketOptionDTO
    {
        [Required]
        public string OptionDescription { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }
    }
}
